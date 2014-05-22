---
layout: post
title: "MSBuild: ItemGroup Metadata Inversion"
---
Sometimes it's useful to treat a piece of metadata as though it were the actual item. This is particularly true if the metadata refers to a file location, so one could pull well-known metadata off the metadata value.

MSBuild does not support metadata having metadata. However, an "inversion" can be performed, where a new ItemGroup is created with the metadata as the primary item entry. The example below also places the original ItemGroup Identity as metadata on the new ItemGroup entries, creating a bidirectional mapping.

{% highlight xml %}
<Project ToolsVersion="3.5" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\ExtensionPack\MSBuild.ExtensionPack.tasks"/>
 
  <ItemGroup>
    <ProjectDefinitions Include="First">
      <ProjectFile>one.sln</ProjectFile>
    </ProjectDefinitions>
    <ProjectDefinitions Include="Second">
      <ProjectFile>two.sln</ProjectFile>
    </ProjectDefinitions>
    <ProjectDefinitions Include="Third">
      <ProjectFile>three.sln</ProjectFile>
    </ProjectDefinitions>
  </ItemGroup>
 
  <Target Name="Default">
    <ItemGroup>
      <ProjectFiles Include="%(ProjectDefinitions.ProjectFile)">
        <ProjectDefinition>@(ProjectDefinitions->'%(Identity)')</ProjectDefinition>
      </ProjectFiles>
    </ItemGroup>
    <Message Text=quot;Project files: @(ProjectFiles) (definitions: @(ProjectFiles->'%(ProjectDefinition)'))"/>
  </Target>
</Project>  
{% endhighlight %}

    Project files: one.sln;two.sln;three.sln (definitions: First;Second;Third)

Note that you do have to watch your grouping; if the metadata being inverted is not unique for all entries in the original ItemGroup, then some entries in the resulting ItemGroup will have multi-valued metadata for their "original Identity" values.

