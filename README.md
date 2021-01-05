# NSMB-Editor
Forked from https://github.com/Mero-Mero/NSMB-Editor originally by https://github.com/Dirbaio/NSMB-Editor

NSMBe 5.3.3 Git, modified to work with MKDS assembly
-----------------
Download NSMBe on the GitHub Release page, older versions available at NSMBHD: https://nsmbhd.net/download/

To run NSMBe:
 - **Windows:** .NET Framework 4.7.2
 - **Linux, MacOSX, Others:** Mono

Necessary NSMBe files and directories:
 - Languages
 - Be.Windows.Forms.HexBox.dll
 - LICENSE.txt
 - NSMBe5.exe

At first bootup time:
 - You will be prompted to download "spritedata.xml", if there isn't one already present. This is a necessary file for proper operation and its download requires the file "classIDforSprite.txt".

Be.Windows.Forms.HexBox:
 - Current version is: 1.6.1

Websites:
 - http://nsmbhd.net/ - The NSMB Hacking Domain, a.k.a NSMBe Forum

Credits:
 - Treeki - Initial developer and maintainer
 - Dirbaio - Current developer and maintainer
 - Piranhaplant - Developer
 - Eloston (ELMario on The NSMB Hacking Domain) - New docs and Be.Windows.Forms.HexBox.dll source code
 - Mero-Mero - Developer
 - RicBent - Developer
 - TheGameratorT - Developer
 - Szymbar - Developer, adopted the structure to work with MKDS assembly
 - And all other contributers!

## Notes about MKDS assembly
In 2012, Gericom wrote [this post right here](https://nsmbhd.net/thread/1025-asm-hacking-project-template/?from=40#20201) that kickstarted the age of custom ASM hacks in Mario Kart DS. For the longest while people were using his old editor, called MKDS Course Modifier (now known as MKDS Utility Belt), but I was missing a few features related to ASM hacking that NSMB-Editor had, but MKDSCM didn't.

This repo aims to bring all of the features of NSMBe back to our MKDS grounds while preserving backwards compatibility with 8 years of history of MKDS hacking until this mod.

Here's a list of available labels and what labels are they equivalent to:
- `ansub` => same as in MKDSCM
- `arepl` => same as in MKDSCM
- `trepl` => same as in MKDSCM
- `tnsub` => 8 bytes, push{lr}, blx (trepl) and pop{pc}
- `ahook` => same as nsmbe's hook

I really didn't want to waste too much time on that xd
But hey, with this we can finally build on what we already have AND use all the features of NSMBe moving forwards in MKDS ASM hacking too!

## Notes about Assembly in general

In order to create an ASM hack, use the Dirbaio's [ASM Patch Template](https://github.com/Dirbaio/ASMPatchTemplate), it's a very nice starting point.
The make file automatizes ASM hacking, but NSMBe can insert labels from built object files into specific spots in the arm9.bin file based on a label instruction that has been specified.

A few examples of labels that are valid for the editor:
- trepl_210ABCD
- nsub_0213928F
- xrpl_2355672NameOfTheActualFunction
- ahook_02345678_Name

A few invalid examples of labels:
- thumbreplace_210ABCD
- nsub-0213928F
- xrpl_1355672NameOfTheActualFunction
- ahook_Name_02345678

A good, albeit scarce example of how the template works and how ASM hacks should be structured within those labels can be found on NSMBHD [here](https://nsmbhd.net/thread/1281-how-asm-hacks-are-setup-tutorial/). This thread serves as gospel in regards to how you should proceed making ASM hacks.

## Examples of projects built using NSMBe 

(version 5.3.2 onwards)
- [Mario Kart DS: Gamecube Grand Prix](https://gbatemp.net/threads/mario-kart-ds-gamecube-grand-prix.485283), a MKDS hack by a team of hackers led by SGC
- [Ermii Kart DS](https://gbatemp.net/threads/ermii-kart-ds-demo-available.428962/), a MKDS hack built to work on MKDSCM's assembler, is 100% forwards compatible with this assembler.

# Previews
<p align="left">
  <img src="https://raw.githubusercontent.com/TheGameratorT/NSMB-Editor/master/NSMBe5/Git_Prevs/filebrowser.png" width="385" title="File Browser">
 <img src="https://raw.githubusercontent.com/TheGameratorT/NSMB-Editor/master/NSMBe5/Git_Prevs/leveleditor.png" width="400" title="Level Editor">
</p>
