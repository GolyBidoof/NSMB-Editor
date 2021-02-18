/*
*   This file is part of NSMB Editor 5.
*
*   NSMB Editor 5 is free software: you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   NSMB Editor 5 is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License
*   along with NSMB Editor 5.  If not, see <http://www.gnu.org/licenses/>.
*/

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;


namespace NSMBe5.Patcher
{
    public class PatchMaker
    {
        public enum Branch : int
        {
            WITHOUT_BRANCH = 0,
            WITH_BRANCH = 1
        }

        public enum Mode : int
        {
            ARM = 0,
            THUMB = 1,
        }

        private int ArenaLoOffs;
        Arm9BinaryHandler handler;
        DirectoryInfo romdir;

        public PatchMaker(DirectoryInfo romdir)
        {
            handler = new Arm9BinaryHandler();
            this.romdir = romdir;
        }

		public uint getCodeAddr()
		{
            handler.load();
            loadArenaLoOffsFile(romdir);
            uint codeAddr = handler.readFromRamAddr(ArenaLoOffs, -1);
            return codeAddr;
		}
		
		public void restore()
		{
            handler.restoreFromBackup();
		}
		
        public void compilePatch()
        {
            uint codeAddr = getCodeAddr();
            PatchCompiler.compilePatch(codeAddr, romdir);
        }

        public void generatePatch(string hookmap = "", string replaces = "")
        {
            int codeAddr = (int)getCodeAddr();
            Console.Out.WriteLine(String.Format("New code address: {0:X8}", codeAddr));

            FileInfo f = new FileInfo(romdir.FullName + "/newcode.bin");
            if (!f.Exists) return;
            FileStream fs = f.OpenRead();

            byte[] newdata = new byte[fs.Length];
            fs.Read(newdata, 0, (int)fs.Length);
            fs.Close();

            ByteArrayOutputStream extradata = new ByteArrayOutputStream();

            extradata.write(newdata);
            extradata.align(4);
            int hookAddr = codeAddr + extradata.getPos();

            #region Decompiled replaces.x code mess
            Console.WriteLine(replaces);
            string str1 = replaces;
            char[] chArray1 = new char[1] { '\n' };
            foreach (string str2 in str1.Split(chArray1))
            {
                char[] chArray2 = new char[1] { ' ' };
                string[] strArray = str2.Split(chArray2);
                if (strArray.Length == 2)
                {
                    strArray[1] = strArray[1].Replace("0x", "");
                    int ov = -1;
                    if (strArray[0].Contains("_ov_"))
                        ov = int.Parse(strArray[0].Substring(strArray[0].IndexOf("_ov_") + 4, 2), NumberStyles.HexNumber);
                    uint uhex = PatchMaker.parseUHex(strArray[1]);
                    this.handler.writeToRamAddr(PatchMaker.parseHex(strArray[0].Substring(0, 8)), uhex, ov);
                }
            }
            FileInfo fileInfo2 = new FileInfo(this.romdir.FullName + "/replaces.x");
            List<PatchMaker.Replace> replaceList = new List<PatchMaker.Replace>();
            if (fileInfo2.Exists)
            {
                StreamReader streamReader = new StreamReader(fileInfo2.FullName, Encoding.UTF8);
                while (!streamReader.EndOfStream)
                {
                    string str2 = streamReader.ReadLine().Replace("\n", "").Replace("\t", "");
                    if (str2.Contains("@"))
                        str2 = str2.Substring(0, str2.IndexOf("@"));
                    List<string> stringList = new List<string>((IEnumerable<string>)str2.Split(' '));
                    for (int index = stringList.Count - 1; index >= 0; --index)
                    {
                        if (stringList[index] == "")
                            stringList.RemoveAt(index);
                    }
                    if (stringList.Count != 0)
                    {
                        if (stringList.Count != 2)
                        {
                            new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong format.\n\nInput line: \"" + str2 + "\"").ShowDialog();
                        }
                        else
                        {
                            int num2 = 9;
                            if (stringList[0].Contains("_ov_"))
                                num2 += 6;
                            if (stringList[0].Length != num2 || !stringList[0].EndsWith(":"))
                            {
                                new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong format.\n\nInput line: \"" + str2 + "\"").ShowDialog();
                            }
                            else
                            {
                                PatchMaker.Replace replace;
                                if (!int.TryParse(str2.Substring(0, 8), NumberStyles.HexNumber, (IFormatProvider)null, out replace.ramAddr))
                                {
                                    new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong address format.\n\nInput address: \"" + str2.Substring(0, 8) + "\"").ShowDialog();
                                }
                                else
                                {
                                    replace.ovId = -1;
                                    if (stringList[0].Contains("_ov_") && !int.TryParse(str2.Substring(str2.IndexOf("_ov_") + 4, 2), NumberStyles.HexNumber, (IFormatProvider)null, out replace.ovId))
                                    {
                                        new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong overlay format.\n\nInput overlay: \"" + str2.Substring(str2.IndexOf("_ov_") + 4, 2) + "\"").ShowDialog();
                                    }
                                    else
                                    {
                                        uint result;
                                        if (uint.TryParse(stringList[1].Replace("0x", ""), NumberStyles.HexNumber, (IFormatProvider)null, out result))
                                        {
                                            this.handler.writeToRamAddr(replace.ramAddr, result, replace.ovId);
                                        }
                                        else
                                        {
                                            replace.funcName = stringList[1];
                                            replaceList.Add(replace);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                streamReader.Close();
            }
            #endregion

            f = new FileInfo(romdir.FullName + "/newcode.sym");
            StreamReader s = f.OpenText();

            while (!s.EndOfStream)
            {
                string l = s.ReadLine();

                #region Decompiled replaces.x code mess
                for (int index = replaceList.Count - 1; index >= 0; --index)
                {
                    PatchMaker.Replace replace = replaceList[index];
                    if (l.Contains(replace.funcName) && (l.Contains(".text") || l.Contains(".data")))
                    {
                        uint result;
                        if (!uint.TryParse(l.Substring(0, 8), NumberStyles.HexNumber, (IFormatProvider)null, out result))
                        {
                            new ErrorMSGBox("", "", "", "Failed parsing destination adress: Wrong adress format.\n\nInput adress: \"" + l.Substring(0, 8) + "\"").ShowDialog();
                        }
                        else
                        {
                            this.handler.writeToRamAddr(replace.ramAddr, result, replace.ovId);
                            replaceList.Remove(replace);
                        }
                    }
                }
                #endregion

                int ind = -1;
                String[,] instructionNames = new String[2,5] {
                    { "ansub", "ahook", "arepl", "trepl", "tnsub" }, //MKDS instructions
                    { "nsub", "hook", "repl", "xrpl", "lrpl" } //NSMB equivalents
                };


                foreach(String instructionName in instructionNames)
                {
                    if (l.Contains(instructionName + "_"))
                    {
                        ind = l.IndexOf(instructionName + "_");
                        break;
                    }
                }

                if (ind != -1)
                {
                    int destRamAddr= parseHex(l.Substring(0, 8));    //Redirect dest addr

                    //Determining if the instruction is 4 or 5 characters long (repl vs arepl, ...)
                    bool isFiveLetterInstruction = l[ind+5]=='_';
                    int startingIndex = isFiveLetterInstruction ? ind + 6 : ind + 5;
                    //Determining if the address is 7 or 8 characters long (200... vs 0200...)
                    bool isEightCharactersAddress = l[startingIndex] == '0';
                    String ramAddress = l.Substring(startingIndex, isEightCharactersAddress ? 8 : 7);
                    if (!isEightCharactersAddress) ramAddress = "0" + ramAddress;

                    int ramAddr = parseHex(ramAddress); //Patched addr
                    uint val = 0;

                    int ovId = -1;
                    if (l.Contains("_ov_"))
                        ovId = parseHex(l.Substring(l.IndexOf("_ov_") + 4, 2));

                    String retAddress;
                    if (l.Contains("_retAddr_"))
                    {
                        startingIndex = l.IndexOf("_retAddr_") + 9;               
                        isEightCharactersAddress = l[startingIndex] == '0';
                        retAddress = l.Substring(startingIndex, isEightCharactersAddress ? 8 : 7);
                        if (!isEightCharactersAddress) retAddress = "0" + retAddress;
                    }
                       

                    string cmd = l.Substring(ind, isFiveLetterInstruction ? 5 : 4 );
                    int thisHookAddr = 0;

                    UInt16 pushLR = 0xB500;
                    UInt16 popPC = 0xBD00;

                    switch (cmd)
                    {
                        case "ansub":
                        case "nsub":
                            val = makeBranchOpcode(ramAddr, destRamAddr, Branch.WITHOUT_BRANCH);
                            break;
                        case "arepl":
                        case "repl":
                            val = makeBranchOpcode(ramAddr, destRamAddr, Branch.WITH_BRANCH);
                            break;
                        case "trepl":
                        case "xrpl":
                            val = makeBranchOpcode(ramAddr, destRamAddr, Branch.WITH_BRANCH, Mode.THUMB);
                            break;
                        case "tnsub":
                        case "lrpl":
                            /*https://www.ece.uvic.ca/~ece255/lab/docs/ARM7-TDMI-manual-pt3.pdf
                                Opcodes for Thumb Push and Pop
                                The goal of this instruction is to push LR/PC before
                                it gets overwritten, then run a custom instruction with a BLX (?)
                                and at the end of it return to the original position.
                                This is essentially an 8-byte tnsub with some extra tinkering around

                                PUSH {R14 (LR/PC))}
                                BLX label
                                POP {R14 (LR/PC)}

                            */
                            handler.writeToRamAddr(ramAddr, pushLR, ovId);
                            ramAddr += 2;
                            handler.writeToRamAddr(ramAddr, makeBranchOpcode(ramAddr, destRamAddr, Branch.WITH_BRANCH, Mode.THUMB), ovId);
                            ramAddr += 4;
                            val = popPC;
                            break;
                        case "ahook":
                        case "hook":
                            //Jump to the hook addr
                            thisHookAddr = hookAddr;
                            val = makeBranchOpcode(ramAddr, hookAddr, 0);

                            uint originalOpcode = handler.readFromRamAddr(ramAddr, ovId);
                            
                            //TODO: Parse and fix original opcode in case of BL instructions
                            //so it's possible to hook over them too.
                            extradata.writeUInt(originalOpcode);
                            hookAddr += 4;
                            extradata.writeUInt(0xE92D5FFF); //push {r0-r12, r14}
                            hookAddr += 4;
                            extradata.writeUInt(makeBranchOpcode(hookAddr, destRamAddr, Branch.WITH_BRANCH));
                            hookAddr += 4;
                            extradata.writeUInt(0xE8BD5FFF); //pop {r0-r12, r14}
                            hookAddr += 4;
                            extradata.writeUInt(makeBranchOpcode(hookAddr, ramAddr+4, Branch.WITHOUT_BRANCH));
                            hookAddr += 4;
                            extradata.writeUInt(0x12345678);
                            hookAddr += 4;
                            break;
                        default:
                            continue;
                    }

                    Console.Out.WriteLine(String.Format("              {0:X8} {1:X8}", destRamAddr, thisHookAddr));

                    handler.writeToRamAddr(ramAddr, val, ovId);
                }
            }

            #region Decompiled replaces.x code mess
            string str4 = hookmap;
            char[] chArray3 = new char[1] { '\n' };
            foreach (string str2 in str4.Split(chArray3))
            {
                if (!(str2 == ""))
                {
                    int hex1 = PatchMaker.parseHex(str2.Substring(str2.Length - 8, 8));
                    int hex2 = PatchMaker.parseHex(str2.Substring(5, 8));
                    int ov = -1;
                    if (str2.Contains("_ov_"))
                        ov = PatchMaker.parseHex(str2.Substring(str2.IndexOf("_ov_") + 4, 2));
                    string str3 = str2.Substring(0, 4);
                    uint val;
                    if (!(str3 == "nsub"))
                    {
                        if (!(str3 == "repl"))
                        {
                            if (str3 == "hook")
                            {
                                val = PatchMaker.makeBranchOpcode(hex2, hookAddr, 0);
                                uint u = this.handler.readFromRamAddr(hex2, ov);
                                extradata.writeUInt(u);
                                int num1 = hookAddr + 4;
                                extradata.writeUInt(3912065023U);
                                int srcAddr1 = num1 + 4;
                                extradata.writeUInt(PatchMaker.makeBranchOpcode(srcAddr1, hex1, Branch.WITH_BRANCH));
                                int num2 = srcAddr1 + 4;
                                extradata.writeUInt(3904724991U);
                                int srcAddr2 = num2 + 4;
                                extradata.writeUInt(PatchMaker.makeBranchOpcode(srcAddr2, hex2 + 4, Branch.WITHOUT_BRANCH));
                                int num3 = srcAddr2 + 4;
                                extradata.writeUInt(305419896U);
                                hookAddr = num3 + 4;
                            }
                            else
                                continue;
                        }
                        else
                            val = PatchMaker.makeBranchOpcode(hex2, hex1, Branch.WITH_BRANCH);
                    }
                    else
                        val = PatchMaker.makeBranchOpcode(hex2, hex1, 0);
                    this.handler.writeToRamAddr(hex2, val, ov);
                }
            }
            string str5 = "";
            foreach (PatchMaker.Replace replace in replaceList)
                str5 = str5 + "\n" + replace.funcName;
            if (str5 != "")
            {
                new ErrorMSGBox("", "", "", "Some replaces were not found:\n" + str5).ShowDialog();
            }
            #endregion

            s.Close();

            int newArenaOffs = codeAddr + extradata.getPos();
            handler.writeToRamAddr(ArenaLoOffs, (uint)newArenaOffs, -1);

            handler.sections.Add(new Arm9BinSection(extradata.getArray(), codeAddr, 0));
            handler.saveSections();
        }

        #region Decompiled replaces.x code mess
        public void insertCode(DirectoryInfo romdir, out string hookmap, out string replaces)
        {
            hookmap = "";
            replaces = "";

            try
            {
                if (!File.Exists(romdir.FullName + @"\bak_safe\main.bin"))
                {
                    if (File.Exists(romdir.FullName + @"\bak\main.bin"))
                    {
                        if (!Directory.Exists(romdir.FullName + @"\bak_safe"))
                            Directory.CreateDirectory(romdir.FullName + @"\bak_safe");
                        File.Copy(romdir.FullName + @"\bak\main.bin", romdir.FullName + @"\bak_safe\main.bin", true);
                    }
                    else
                        this.handler.makeBinBackup(-1, true);
                }
                File.Copy(romdir.FullName + @"\bak_safe\main.bin", romdir.FullName + @"\bak\main.bin", true);
            }
            catch (Exception ex)
            {
                new ErrorMSGBox("", "", "", ex.ToString()).ShowDialog();
            }

            FileInfo fileInfo1 = new FileInfo(romdir.FullName + "/codemap.x");
            if (!fileInfo1.Exists)
                return;
            StreamReader streamReader1 = new StreamReader(fileInfo1.FullName, Encoding.UTF8);
            while (!streamReader1.EndOfStream)
            {
                string str1 = streamReader1.ReadLine();
                string[] strArray = str1.Split(' ');
                if (strArray.Length != 2 && strArray.Length != 3)
                {
                    new ErrorMSGBox("", "", "", "Error: Wrong Codemap line format (" + str1 + ")").ShowDialog();
                }
                else
                {
                    string str2 = strArray[0];
                    if (!Directory.Exists(romdir.FullName + "/" + str2))
                    {
                        new ErrorMSGBox("", "", "", "Error: Directory \"" + str2 + "\" does not exist").ShowDialog();
                    }
                    else
                    {
                        if (strArray[1].StartsWith("0x"))
                            strArray[1] = strArray[1].Substring(2);
                        if (strArray[2].StartsWith("0x"))
                            strArray[2] = strArray[2].Substring(2);
                        int hex = PatchMaker.parseHex(strArray[1]);
                        if (hex < 33554432)
                        {
                            new ErrorMSGBox("", "", "", "Error: Destination address of " + strArray[0] + " is too small (0x" + hex.ToString("X8") + ")").ShowDialog();
                        }
                        else if (hex > 50331647)
                        {
                            new ErrorMSGBox("", "", "", "Error: Destination address of " + strArray[0] + " is too big (0x" + hex.ToString("X8") + ")").ShowDialog();
                        }
                        else
                        {
                            int num5 = -1;
                            if (strArray.Length == 3)
                                num5 = PatchMaker.parseHex(strArray[2]);
                            PatchCompiler.runProcess("make clean", romdir.FullName + "/" + strArray[0]);
                            if (PatchCompiler.runProcess("make CODEADDR=0x" + hex.ToString("X8"), romdir.FullName + "/" + strArray[0]) != 0)
                            {
                                new ErrorMSGBox("", "", "", "Error: Compilation of " + strArray[0] + " failed.").ShowDialog();
                            }
                            else
                            {
                                string str3 = romdir.FullName + "/" + str2 + "/newcode.bin";
                                if (!File.Exists(str3))
                                {
                                    new ErrorMSGBox("", "", "", "Insert Code Binary \"" + str3 + "\" does not exist!").ShowDialog();
                                    break;
                                }
                                long length1 = new FileInfo(str3).Length;
                                if (length1 > (long)num5)
                                {
                                    new ErrorMSGBox("", "", "", "Insert Code Binary \"" + str3 + "\" size (" + (object)length1 + ") exeeds given maximum size (" + (object)num5 + ")").ShowDialog();
                                    break;
                                }
                                FileInfo fileInfo2 = new FileInfo(romdir.FullName + "/" + strArray[0] + "/replaces.x");
                                List<PatchMaker.Replace> replaceList = new List<PatchMaker.Replace>();
                                if (fileInfo2.Exists)
                                {
                                    StreamReader streamReader2 = new StreamReader(fileInfo2.FullName, Encoding.UTF8);
                                    while (!streamReader2.EndOfStream)
                                    {
                                        string str4 = streamReader2.ReadLine().Replace("\n", "").Replace("\t", "");
                                        if (str4.Contains("@"))
                                            str4 = str4.Substring(0, str4.IndexOf("@"));
                                        List<string> stringList = new List<string>((IEnumerable<string>)str4.Split(' '));
                                        for (int index = stringList.Count - 1; index >= 0; --index)
                                        {
                                            if (stringList[index] == "")
                                                stringList.RemoveAt(index);
                                        }
                                        if (stringList.Count != 0)
                                        {
                                            if (stringList.Count != 2)
                                            {
                                                new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong format.\n\nInput line: \"" + str4 + "\"").ShowDialog();
                                            }
                                            else
                                            {
                                                int num8 = 9;
                                                if (stringList[0].Contains("_ov_"))
                                                    num8 += 6;
                                                if (stringList[0].Length != num8 || !stringList[0].EndsWith(":"))
                                                {
                                                    new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong format.\n\nInput line: \"" + str4 + "\"").ShowDialog();
                                                }
                                                else
                                                {
                                                    PatchMaker.Replace replace;
                                                    if (!int.TryParse(str4.Substring(0, 8), NumberStyles.HexNumber, (IFormatProvider)null, out replace.ramAddr))
                                                    {
                                                        new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong address format.\n\nInput address: \"" + str4.Substring(0, 8) + "\"").ShowDialog();
                                                    }
                                                    else
                                                    {
                                                        replace.ovId = -1;
                                                        if (stringList[0].Contains("_ov_") && !int.TryParse(str4.Substring(str4.IndexOf("_ov_") + 4, 2), NumberStyles.HexNumber, (IFormatProvider)null, out replace.ovId))
                                                        {
                                                            new ErrorMSGBox("", "", "", "Failed parsing replace: Wrong overlay format.\n\nInput overlay: \"" + str4.Substring(str4.IndexOf("_ov_") + 4, 2) + "\"").ShowDialog();
                                                        }
                                                        else
                                                        {
                                                            uint result;
                                                            if (uint.TryParse(stringList[1].Replace("0x", ""), NumberStyles.HexNumber, (IFormatProvider)null, out result))
                                                            {
                                                                replaces = replaces + str4 + "\n";
                                                            }
                                                            else
                                                            {
                                                                replace.funcName = stringList[1];
                                                                replaceList.Add(replace);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    streamReader2.Close();
                                }
                                StreamReader streamReader3 = new StreamReader(romdir.FullName + "/" + strArray[0] + "/newcode.sym", Encoding.UTF8);
                                while (!streamReader3.EndOfStream)
                                {
                                    string str4 = streamReader3.ReadLine();
                                    for (int index = replaceList.Count - 1; index >= 0; --index)
                                    {
                                        PatchMaker.Replace replace = replaceList[index];
                                        if (str4.Contains(replace.funcName) && (str4.Contains(".text") || str4.Contains(".data")))
                                        {
                                            uint result;
                                            if (!uint.TryParse(str4.Substring(0, 8), NumberStyles.HexNumber, (IFormatProvider)null, out result))
                                            {
                                                new ErrorMSGBox("", "", "", "Failed parsing destination adress: Wrong adress format.\n\nInput adress: \"" + str4.Substring(0, 8) + "\"").ShowDialog();
                                            }
                                            else
                                            {
                                                if (replace.ovId == -1)
                                                    replaces = replaces + replace.ramAddr.ToString("X8") + " 0x" + result.ToString("X8") + "\n";
                                                else
                                                    replaces = replaces + replace.ramAddr.ToString("X8") + "_ov_" + replace.ovId.ToString("X2") + " 0x" + result.ToString("X8") + "\n";
                                                replaceList.Remove(replace);
                                            }
                                        }
                                    }
                                    int startIndex = -1;
                                    if (str4.Contains("nsub_"))
                                        startIndex = str4.IndexOf("nsub_");
                                    if (str4.Contains("hook_"))
                                        startIndex = str4.IndexOf("hook_");
                                    if (str4.Contains("repl_"))
                                        startIndex = str4.IndexOf("repl_");
                                    if (startIndex != -1)
                                    {
                                        string str5 = str4.Substring(startIndex);
                                        int length2 = 13;
                                        if (str5.Contains("_ov_"))
                                            length2 += 6;
                                        else if (str5.Contains("_main"))
                                            length2 += 5;
                                        string str6 = str5.Substring(0, length2);
                                        hookmap = hookmap + str6 + ": " + str4.Substring(0, 8) + "\n";
                                    }
                                }
                                streamReader3.Close();
                                string str7 = "";
                                foreach (PatchMaker.Replace replace in replaceList)
                                    str7 = str7 + "\n" + replace.funcName;
                                if (str7 != "")
                                {
                                    new ErrorMSGBox("", "", "", "Some replaces were not found:\n" + str7).ShowDialog();
                                }
                                string path = romdir.FullName + "/bak/main.bin";
                                if (!File.Exists(path))
                                {
                                    new ErrorMSGBox("", "", "", "ARM 9 Binary \"" + path + "\" does not exist!").ShowDialog();
                                    break;
                                }
                                byte[] bytes = File.ReadAllBytes(romdir.ToString() + "/bak/main.bin");
                                byte[] numArray = File.ReadAllBytes(str3);
                                Array.Copy((Array)numArray, 0, (Array)bytes, hex - 33554432, numArray.Length);
                                File.WriteAllBytes(path, bytes);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        private void loadArenaLoOffsFile(DirectoryInfo romdir)
        {
            FileInfo f = new FileInfo(romdir.FullName + "/arenaoffs.txt");
            StreamReader s = f.OpenText();
            string l = s.ReadLine();
            ArenaLoOffs = int.Parse(l, System.Globalization.NumberStyles.HexNumber);
            s.Close();
        }


        public static uint makeBranchOpcode(int srcAddr, int destAddr, Branch branch, Mode mode = Mode.ARM)
        {
            unchecked
            {
                uint res;
                if (mode == Mode.ARM)
                {
                    /* ARM */
                    res = (uint)0xEA000000;

                    if (branch == Branch.WITH_BRANCH)
                        res |= 0x01000000;

                    int offs = (destAddr / 4) - (srcAddr / 4) - 2;
                    offs &= 0x00FFFFFF;

                    res |= (uint)offs;
                } else 
                {
                    /* Thumb has to jump with a branch */
                    UInt16 res1 = 0xF000;
                    UInt16 res2 = 0xE800;

                    int offs = destAddr - srcAddr - 2;
                    offs >>= 2;
                    offs &= 0x003FFFFF;
                    
                    res1 |= (UInt16)((offs >> 10) & 0x7FF);
                    res2 |= (UInt16)((offs << 1)  & 0x7FF);

                    res = (uint)(((uint)res2 << 16) | res1);
                }
                return res;
            }
        }


        public static uint parseUHex(string s)
        {
            return uint.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        public static int parseHex(string s)
        {
            return int.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        private struct Replace
        {
            public int ramAddr;
            public int ovId;
            public string funcName;
        }
    }
}
