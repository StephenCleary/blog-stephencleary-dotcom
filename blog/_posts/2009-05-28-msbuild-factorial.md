---
layout: post
title: "MSBuild: Factorial!"
---
I've been doing some exploring of MSBuild as a programming language. There are some interesting results regarding mutability/immutability, but that's for another post.



This post is about functions. In particular, a Target may be invoked using the MSBuild task, so I'm exploring using Targets as functions. MSBuild can pass parameters to a Target by sending it Properties. Property changes are not propogated back to the caller, though, so getting a return value is a bit trickier.



It turns out that MSBuild does return one bit of information from a Target: its Outputs. It's possible to set the Outputs of a Target to a Property, and have that Target depend on another Target that sets that Property. In this way, it is possible to create a pair of Targets that can "calculate" the outer Target's Outputs.



By combining these approaches (setting Properties for arguments, and using the Target's Outputs as a return value), it is possible to treat a Target as a function.



To demonstrate, I wrote this program, which uses MSBuild to recursively calculate the factorial of the $(Input) property. Have fun playing!



{% highlight xml %}<Project ToolsVersion="3.5" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\ExtensionPack\MSBuild.ExtensionPack.tasks"/>

  <!-- Factorial program using MSBuild recursively -->

  <Target Name="Default">
    <!-- Display usage -->
    <Error Condition="'$(Input)' == ''" Text="Usage: msbuild factorial.proj [/nologo] [/clp:v=minimal] /p:Input=nnn"/>

    <!-- Argument error checking -->
    <MSBuild.ExtensionPack.Science.Maths TaskAction="Compare" P1="$(Input)" P2="1" Comparison="LessThan">
      <Output TaskParameter="LogicalResult" PropertyName="InputCheck"/>
    </MSBuild.ExtensionPack.Science.Maths>
    <Error Condition="'$(InputCheck)' != 'False'" Text="Input cannot be less than 1."/>

    <!-- Invoke the Factorial target with the current Input property -->
    <MSBuild Projects="$(MSBuildProjectFile)" Targets="Factorial" Properties="Input=$(Input)">
      <Output TaskParameter="TargetOutputs" ItemName="FactorialResult"/>
    </MSBuild>

    <!-- Display the result -->
    <Message Importance="high" Text="Result: @(FactorialResult)"/>
  </Target>

  <!-- The Factorial target uses FactorialCore to do the calculation, storing the result in FactorialResult -->
  <Target Name="Factorial" DependsOnTargets="FactorialCore" Outputs="$(FactorialResult)" />

  <Target Name="FactorialCore">
    <!-- If the input is 1, then the factorial is 1 -->
    <PropertyGroup Condition="'$(Input)' == '1'">
      <FactorialResult>1</FactorialResult>
    </PropertyGroup>

    <!-- If we don't know the result yet (i.e., the input is not 1), then calculate the factorial -->
    <CallTarget Condition="'$(FactorialResult)' == ''" Targets="CalculateFactorial"/>
  </Target>

  <Target Name="CalculateFactorial">
    <!-- Subtract 1 from $(Input) -->
    <MSBuild.ExtensionPack.Science.Maths TaskAction="Subtract" Numbers="$(Input);1">
      <Output TaskParameter="Result" PropertyName="InputMinus1"/>
    </MSBuild.ExtensionPack.Science.Maths>

    <!-- Determine the factorial of $(Input) - 1 -->
    <MSBuild Projects="$(MSBuildProjectFile)" Targets="Factorial" Properties="Input=$(InputMinus1)">
      <Output TaskParameter="TargetOutputs" ItemName="SubResult"/>
    </MSBuild>

    <!-- Multiply !($(Input) - 1) by $(Input) to get the result-->
    <MSBuild.ExtensionPack.Science.Maths TaskAction="Multiply" Numbers="@(SubResult);$(Input)">
      <Output TaskParameter="Result" PropertyName="FactorialResult"/>
    </MSBuild.ExtensionPack.Science.Maths>
  </Target>

  <!-- Maybe I just have way too much time on my hands... -->
</Project>
{% endhighlight %}

**msbuild factorial.proj /nologo /clp:v=minimal /p:Input=5**


> Default:  
> 
> &nbsp; Result: 120


**msbuild factorial.proj /nologo /clp:v=minimal /p:Input=7**


> Default:  
> 
> &nbsp; Result: 5040


Useless, but cool nonetheless.

