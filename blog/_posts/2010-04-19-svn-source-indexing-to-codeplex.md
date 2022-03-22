---
layout: post
title: "SVN Source Indexing to CodePlex"
---
Open-source libraries naturally come with source code. However, the source is often not easy to use; compiling from source usually includes duplicating another programmer's toolset. Most programmers who use open-source libraries only use the executables, and ignore the source. They act as a "consumer programmer," a technologically-savvy end-user of the library, which was created by a "producer programmer."

When the consumer programmer is debugging, it would certainly be nice to step into the producer programmer's source code. Microsoft has enabled this for many of the .NET source files in their libraries. There is a way to enable a similar capability for open-source libraries as well.

## The PDB

The symbol file (PDB) for an executable (DLL) includes information about where the original source files were (see [PDB Files: What Every Developer Must Know](http://www.wintellect.com/CS/blogs/jrobbins/archive/2009/05/11/pdb-files-what-every-developer-must-know.aspx), [Source Server Helps You Kill Bugs Dead](http://msdn.microsoft.com/en-us/magazine/cc163563.aspx?WT.mc_id=DT-MVP-5000058), and of course the [Debugging .NET Apps](http://www.amazon.com/gp/product/0735622027?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735622027) book from the bugslayer man himself). Normally, this is just a simple file path, so the source file will only be found if the producer programmer is debugging his own code. The consumer programmer is out of luck.

However, there's a way for the producer programmer to add information to the PDB; specifically, he can add instructions to the PDB to check files out of version control on demand. This works great for the consumer programmer if the producer programmer uses a public source control server like CodePlex. In this case, if the consumer programmer needs to debug the library code, they can literally just step into it, and Visual Studio will automatically check out the correct source file from the matching revision and load it into its workspace!

There's a bit of setup to be done, and it's not _quite_ automatic (there are a few prompts for security reasons). But it is still cool.

## The Producer Programmer: Distribute Source-Indexed PDBs

The programmer who is developing the library must distribute "source-indexed" PDBs along with his library DLLs. A "source-indexed" PDB is a PDB that has extra information so it knows how to check out the appropriate source file. There are a few installation prerequisites for the producer programmer:

- [Debugging Tools for Windows](http://www.microsoft.com/whdc/Devtools/Debugging/default.mspx) - This includes the executables, command scripts, and Perl scripts necessary to source-index a PDB.
- [Perl](http://www.activestate.com/activeperl/) - Necessary to run the Perl scripts.
- svn.exe (in %PATH%) - This is usually installed (and placed in %PATH%) by a [Subversion binary package](http://subversion.apache.org/packages.html#windows).

When the producer programmer is ready to create a release of his library, he follows these steps:

 1. Build the library binaries (and PDBs).
 1. Ensure all source code is checked in.
 1. Source-index the PDBs; assuming the PDBs are in the "..\Binaries" directory and DTfW was installed in "c:\Program Files\Debugging Tools for Windows (x86)":  

_"c:\Program Files\Debugging Tools for Windows (x86)\srcsrv\ssindex.cmd" /SYSTEM=SVN /SYMBOLS=..\Binaries /Debug_

All the PDBs in "..\Binaries" are updated in-place to point to the current source in source control. They are now ready to be released (note that the PDBs should be included in every release along with DLLs and XML documentation files).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note that the "/Debug" switch is just an output verbosity option for SSIndex.cmd. The output from SSIndex should include a "wrote stream" message for each PDB. If there is anything in the output that looks like "[ERROR]", "zero source files found", or if no PDB files were found, then the source indexing was **not** successful for those PDB files.
</div>

The following command can be used to verify that the PDB was correctly source indexed:  

_"c:\Program Files\Debugging Tools for Windows (x86)\srcsrv\srctool.exe" MyLibrary.pdb_  

If the PDB is not source indexed, srctool will simply print "MyLibrary.pdb is not source indexed". If it is source indexed, then it will display all the source server commands that will retrieve the correct source files.

## The Consumer Programmer: Allow Source-Indexed PDBs

The consumer programmer does have an installation prerequisite; since Visual Studio will use svn.exe to retrieve the source files, it must first be installed:

  - svn.exe (in %PATH%) - This is usually installed (and placed in %PATH%) by a [Subversion binary package](http://subversion.apache.org/packages.html#windows).

Once svn.exe is in %PATH%, the consumer programmer may enable source-indexed PDBs by checking the following option box in Visual Studio 2010: Options -> Debugging -> General -> Enable source server support.

At this point, a consumer programmer may step into the library code, and (after prompting for permission) Visual Studio will download the correct source file and allow stepping through the source.

