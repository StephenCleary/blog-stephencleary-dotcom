---
layout: post
title: "VS2010 Without Web Deployment Projects"
---
Visual Studio 2010 was released yesterday, as just about everyone knows. I didn't find out until tonight that the [Beta 1 of WDP for VS2010](http://www.microsoft.com/downloads/details.aspx?FamilyID=711a2eef-b107-4784-9063-c978edc498cd) is also available.



I've been working on getting the [church website](http://www.landmarkbaptist.ws/) moved to the new platform. It has gone fairly smoothly, aside from a [compilation error](http://forums.asp.net/t/1546705.aspx) which forced me to move the EF model into a separate assembly (but since that's best practice anyway, I didn't mind).



## Excluding Files

We have limited bandwidth at home (Northern Michigan, remember...), so I've used WDP in the past to exclude many of the files during deployment (pdbs and xmldoc in particular). VS2010 has greatly enhanced deployment support, but it doesn't quite cover WDP's feature set.



VS2010 will ignore pdb files if you check the "Exclude generated debug symbols" under the "Package/Publish Web" tab. Other files are still thrown in the mix, though, unless you add this to your project file:




  <PropertyGroup>
    <ExcludeFilesFromPackage>true</ExcludeFilesFromPackage>
  </PropertyGroup>
  <ItemGroup>
    <ExcludeFromPackageFiles Include="$(ProjectDir)bin\*.xml">
      <FromTarget>Project</FromTarget>
    </ExcludeFromPackageFiles>
  </ItemGroup>


This is very similar to WDP's ExcludeFromBuild item group that uses the SourceWebPhysicalPath property. There are two major differences: the ExcludeFilesFromPackage property needs to be set (otherwise, the ExcludeFromPackageFiles item group would be ignored), and the ProjectDir property ends in a backslash (whereas the SourceWebPhysicalPath property did not).



## Precompiling (sort of)

VS2010 does not have the option of precompiling ASP.NET web applications (it will, however, precompile ASP.NET web sites). This is sad, but there is sort-of a workaround.



It's possible to force MVC views to be precompiled by placing this in a PropertyGroup in the project file (I prefer placing it in the PropertyGroup with the condition "Release|AnyCPU", so that it only takes effect on release builds):




<MvcBuildViews>true</MvcBuildViews>


This will cause the MVC views to be compiled, catching all compile-time errors at build time. Unfortunately, the compiled views are then thrown away instead of being merged into the web application assembly.



Full precompiling (and merging with the web application) is available in [WDP Beta 1](http://www.microsoft.com/downloads/details.aspx?FamilyID=711a2eef-b107-4784-9063-c978edc498cd). Or, if you enjoy painful programming, you can do it yourself in MSBuild. ;)

