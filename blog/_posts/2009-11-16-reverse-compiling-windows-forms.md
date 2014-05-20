---
layout: post
title: "Reverse Compiling Windows Forms"
---
Today I had a fun task: the source code for an existing executable had been lost, and I got the job of getting it back. The good news is that [Red Gate's Reflector](http://www.red-gate.com/products/reflector/) (formerly Lutz Roeder's Reflector) is a standard tool for any serious .NET programmer, and it does quite a decent job of decompiling (nonobfuscated) .NET code. The bad news is that I had to also reverse-engineer the GUI.

After finding nothing on Google, and a bit of trial and error, I discovered the following procedure worked adequately, at least for my (simple) executable on Visual Studio 2008:

1. First, export the source from Reflector, create a solution, and ensure it builds.
1. Convert the ".resources" files into ".resx" files. Reflector just dumps out the binary .NET resources, but VS prefers them as XML. Fire up your VS command prompt and run this command: "resgen My.Long.Resource.Name.resources Name.resx".
1. Move the resulting ".resx" files into their appropriate directories (e.g., "My\Long\Resource"). The rest of these steps must be done for each ".resx" file.
1. Add the ".resx" files to your solution (they should be inserted under the matching ".cs" file), remove the old ".resources" file from the solution, and rebuild.
1. Add a new empty C# code file named "Name.Designer.cs" file in the same directory, and paste in the following code:
{% highlight csharp %}
namespace My.Long.Resource
{
    partial class Name
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
 
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
 
        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
 
        #endregion
 
    }
}
{% endhighlight %}

1. Open up the parent "Name.cs" file (right-click -> View Code) and add the "partial" attribute to its class declaration.
1. Delete the member variable "components".
1. Move all GUI member variables from "Name.cs" to the end of "Name.Designer.cs" (placing them after the "#endregion"). GUI member variables are anything that is added from the Toolbox, so that would include System.Windows.Forms.Timer components, etc.
1. Delete the "Name.Dispose" method from the "Name.cs" file.
1. Move "Name.InitializeComponent" from the "Name.cs" file into the "Name.Designer.cs" file, placing it before the "#endregion".
1. For each unrecognized type in the member variables and InitializeComponent, either fully qualify it or add a using declaration. Fully qualifying each type is more time consuming, but matches exactly what the designer expects. After this step, the solution should build.
1. If InitializeComponent contains a line assigning the member variable "this.components = new Container();", then it must be changed to be "this.components = new System.ComponentModel.Container();" _and_ moved to the top of the method.
1. If InitializeComponent contains a line creating a resource manager, e.g., "ComponentResourceManager manager = new ComponentResourceManager(typeof(Name));", the local variable "manager" _must_ be renamed to "resources" (and update references to the renamed object).
1. Repeat attempting to load it in the designer, fully qualifying any types that it complains about (this step is necessary because the designer's code parser is not as smart as the C# compiler):

 - "The designer cannot process the code..." - Any enum member variables that have the same name as their type need to have their value fully qualified, e.g., "base.AutoScaleMode = AutoScaleMode.Font;" needs to be "base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;").
 - "The variable ... is either undeclared or was never assigned" - Many types seem to require fully qualified type names when declared (e.g., "private OpenFileDialog openFileDialog;" needs to be "private System.Windows.Forms.OpenFileDialog openFileDialog;").

After following the (rather tedious) procedure above, you should have a form that can be opened in the VS designer. If I had more time, I'd wrap it up as a Reflector add-in, but time seems to be a fleeting resource these days.

