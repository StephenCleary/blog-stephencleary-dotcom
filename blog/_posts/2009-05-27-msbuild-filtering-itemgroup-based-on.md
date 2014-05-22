---
layout: post
title: "MSBuild: Filtering an ItemGroup based on a Property"
---
I started playing with MSBuild this weekend. It's a little under-documented for my taste, but seems rather powerful. It has a strange combination of functional and procedural styles which make some simple tasks relatively complex.

This is the first in what I hope will be a series of posts of solutions that I've worked through for MSBuild. Keep in mind that I am still an MSBuild beginner, so there may be a better way to solve these problems.

## The Problem

Given one ItemGroup (including metadata), how can one choose a subset of the items, keeping metadata intact? The subset is determined by a property that is actually a list of keys.

## The Solution

{% highlight xml %}
<Project ToolsVersion="3.5" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\ExtensionPack\MSBuild.ExtensionPack.tasks"/>
 
  <ItemGroup>
    <ProjectDefinitions Include="First">
      <Argument>1</Argument>
    </ProjectDefinitions>
    <ProjectDefinitions Include="Second">
      <Argument>2</Argument>
    </ProjectDefinitions>
    <ProjectDefinitions Include="Third">
      <Argument>3</Argument>
    </ProjectDefinitions>
  </ItemGroup>
 
  <PropertyGroup>
    <!-- By default, only build the first and third projects; this property may be overridden on the command line with the "/p" argument -->
    <Projects>First;Third</Projects>
  </PropertyGroup>
 
  <Target Name="Default" DependsOnTargets="DetermineProjectsToBuild">
    <Message Text="Projects to build: @(Projects) (arguments: @(Projects->'%(Argument)'))"/>
  </Target>
  
  <!--
  Determines which projects to build, based off the ProjectDefinitions items and the Projects property. Calculates the following item group:
    Projects - containing all ProjectDefinitions specified in the Projects property, with all metadata intact.
  -->
  <Target Name="DetermineProjectsToBuild">
    <!-- Split the Projects property up into an item group ProjectNamesToBuild that has one entry per item name -->
    <MSBuild.ExtensionPack.Framework.MSBuildHelper TaskAction="StringToItemCol" ItemString="$(Projects)" Separator=";">
      <Output TaskParameter="OutputItems" ItemName="ProjectNamesToBuild"/>
    </MSBuild.ExtensionPack.Framework.MSBuildHelper>
 
    <!-- Build the Projects item group by looking up the project names in the ProjectDefinitions item group -->
    <FindInList CaseSensitive="false" List="@(ProjectDefinitions)" ItemSpecToFind="%(ProjectNamesToBuild.Identity)">
      <Output TaskParameter="ItemFound" ItemName="Projects"/>
    </FindInList>
  </Target>
</Project>  
{% endhighlight %}

    Projects to build: First;Third (arguments: 1;3)

By default, this example decides to build the first and third projects. However, passing _/p:Projects="First;Second"_ will change to the first and second projects (shown below). The metadata is preserved, as shown by displaying the arguments.

    Projects to build: First;Second (arguments: 1;2)
