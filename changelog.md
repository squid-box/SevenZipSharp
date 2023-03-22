# Changelog

## 1.5.2 (2023-03-22)
- Fixed an issue when seeking in streams with SeekOrigin.End, thanks to GitHub user bneidhold.
- Fixed an issue when checking multi-volume 7z archives, thanks to GitHub user panda73111.
- Changed CI from AppVeyor to GitHub Actions.
- .NET Framework version bumped from 4.5 to 4.7.2.

## 1.5.0 (2021-08-15)
- Added separate NuGet (Lite) excluding creation of self-extracting archives.

## 1.4.0 (2021-04-12)
- Added a work-around to allow UWP apps to use SevenZipSharp in Release builds.
- Fix issue limiting split archive volume size to 2GB.

## 1.3.318 (2021-03-05)
- Fixed issue with SFN paths during compression.
- Fixed issue with library path detection during initialization, thanks to GitHub user thoemmi.
- Fixed issue detecting .deb archives, thanks to GitHub user NickHarmer.
- Added ability to skip files during archive extraction, thanks to GitHub user NickHarmer.

## 1.3.283 (2020-05-01)
- Added awaitable extraction functions, thanks to GitHub user kikijiki.
- Added awaitable compression functions.

## 1.2.265 (2020-03-21)
- Fixed problem where a UNC destination path would fail extraction.

## 1.2.258 (2020-02-21)
- Fixed several bugs in custom parameters for the SevenZipCompressor class.

## 1.2.242 (2019-12-24)
- Improved Operation Result handling and better performance by removing excessive GC, thanks to GitHub user zylab-official.

## 1.2.231 (2019-10-05)
- Improved support for non-solid extraction of 7z "Copy" file from solid archive using index, thanks to GitHub user Mgamerz.
- Fixed issue related to invalid character in directory and file name, thanks to GitHub user Ramkaran Yadav.

## 1.2.219 (2019-09-19)
- Fixed issue when SevenZipSharp is embedded, thanks to GitHub user Mgamerz.

## 1.2.201 (2019-09-12)
- Added NuGet dependencies for .NET Standard 2.0 projects. 
- Improved error handling.

## 1.2.191 (2019-09-12)
- Re-added support for detecting RAR4 sfx archives.

## 1.2.184 (2019-06-08)
- Now supports both .NET45 and .NET Standard 2.0, thanks to Github user frblondin.

## 1.1.144 (2019-04-20)
- Now accepts IDictionaries where applicable, thanks to GitHub user rotvel.

## 1.1.136 (2019-03-05)
- Archive property VolumeIndex now exposed through SevenZipSharp, thanks to Ravi Patel (ravibpatel).

## 1.1.126 (2019-02-06)
- Improved error output a little, no new functionality or bugfixes.

## 1.1.106 (2018-08-13)
- Fixed problem when modifying encrypted archives.

## 1.1.96 (2018-08-01)
- Added support for new archive formats supported by 7-Zip (thanks to Artem Tarasov).

## 1.1.91 (2018-07-31)
- Fixed problems with creating self-extracting archives.

## 1.1.53 (2018-07-27)
- Fixed bugs when: 
  + extracting .gz archives
  + extracting .bz2 archives
  + extracting zip file from stream
  + compressing a stream into another stream

## 1.1.22 (2018-07-22)
- Includes support for extracting RAR v5 archives.
