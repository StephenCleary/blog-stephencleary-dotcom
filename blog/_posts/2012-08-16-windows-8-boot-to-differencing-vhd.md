---
layout: post
title: "Windows 8 Boot to Differencing VHD"
---
Last night I installed Windows 8, booting to a differencing vhd. This is a bit of an advanced setup, so I thought I'd quickly type up my reasoning for this approach and the steps taken. Of course, you do this at your own risk, etc, etc. I also am assuming that you have a currently working Windows 7 or Windows 8 Preview/Beta installation.

We only have one computer in our house. Sometimes it's used for work, and sometimes it's used for entertainment. I wanted to have a system that had minimal software for work, but was also available for the family to use.

## Considering Hyper-V

One of the (many) hallmarks of Windows 8 is that [Microsoft is providing client Hyper-V](https://docs.microsoft.com/en-us/archive/blogs/b8/bringing-hyper-v-to-windows-8). Using Hyper-V, I could run multiple virtual computers at the same time. Hyper-V (like every virtualization system) also has a nice "snapshot" feature. However, I decided not to go with Hyper-V because it's not a very seamless experience - e.g., for USB devices to work, you have to actually remote desktop into the VM.

I made the decision to boot from VHD instead. With this approach, only one OS can be running at a time - the only thing virtualized is the disk access, and it's virtualized within the OS itself, not by another full layer (like Hyper-V). So, boot from VHD runs faster and you get full access to hardware, but you lose the multiple-VMs-running-at-once that you can get from Hyper-V. Also, you can use snapshots with boot-from-VHD, but they're not as easy to use as snapshots in Hyper-V.

Everyone's needs are different, so choose wisely.

## Emulating Snapshots with Differencing VHDs

VHD stands for Virtual Hard Disk, and that's just what it is: a disk file that contains a full hard disk. Usually, you'll want to create a "dynamic VHD", which only takes up as much physical disk space as it needs to; empty parts of the VHD are not actually saved to disk.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Terminology alert: "dynamic VHD" is completely different than "dynamic disk".
</div>

There's another type of VHD: a "differencing VHD". This is a "child" VHD that only saves the _differences_ from its _parent_ VHD. We can make this work kind of like a snapshot.

Here's where I'd like to end up: one VHD that is just the OS freshly installed (call it "Win8-base.vhd"). Then create a child VHD that is the OS activated with basic setups: locale, family user accounts, really basic stuff like that (call it "Win8.vhd"). Finally, I'll create two child VHDs of that one: one for my development ("Win8Dev.vhd") and one for family entertainment ("Win8Ent.vhd").

That way, all my development tools will live on "Win8Dev.vhd". In a couple of years when VS2014 (or whatever) comes out, I can just nuke it and create another child VHD with the new toolset.

Likewise, all the entertainment programs go on "Win8Ent.vhd". This one won't be nuked unless it gets into a really bad state, but it will have many programs installed and uninstalled over the years. And I prefer to keep that isolated from my development setup.

You can create and delete differencing VHDs at will. You usually don't end up doing this a lot, though, because there isn't a slick UI for it (yet).

## Step 1 - Install Windows 8

By far, the easiest way to install Windows 8 on a VHD is to first boot into Windows 8. Yeah, I'm not kidding, unfortunately - it's a real chicken-and-egg problem. There are [ways to do this](http://www.hanselman.com/blog/GuideToInstallingAndBootingWindows8DeveloperPreviewOffAVHDVirtualHardDisk.aspx) by breaking into a command prompt during a Windows 8 installation while the option is right in front of you to destroy your current OS, but that approach just seems unnecessarily risky compared to this one.

If you've already got Windows 8 RTM (as of the time of this writing), then you (or a friend) probably have Windows 8 Preview/Beta installed somewhere, and you can use that. Otherwise, fire up [VMWare](http://www.vmware.com/products/player/) or [VirtualBox](https://www.virtualbox.org/) and install Windows 8.

## Step 2 - Use Convert-WindowsImage

Once you have Windows 8 installed (virtually, if necessary), download the excellent [Convert-WindowsImage](http://gallery.technet.microsoft.com/scriptcenter/Convert-WindowsImageps1-0fe23a8f) PowerShell script (remember to [unblock](http://support.microsoft.com/kb/883260) it and [Set-ExecutionPolicy RemoteSigned](http://technet.microsoft.com/en-us/library/ee176961.aspx), or the OS won't let you run it).

I like to run Convert-WindowsImage with a user interface. To do this, hit the Windows key, type "powershell", right-click PowerShell and choose "Run as Administrator". Then you can change to the directory containing the script file and execute ".\Convert-WindowsImage.ps1 -ShowUI".

The user interface is pretty self-explanatory. You choose an input file (e.g., your Windows 8 ISO), select an edition, make sure the VHD is large enough (remember, a dynamic VHD won't actually take up that much disk space unless it is full), choose where to save the VHD, and click the big button. If you don't choose the VHD file name, this script will give it some huge name and stick it on your desktop.

Get coffee. If you're running virtually, this will take a really long time (several hours for me).

One more thing: Windows 8 will helpfully offer to format the VHD for you. Do **not** do this; Convert-WindowsImage will format it.

## Step 3 (optional) - Create Differencing VHD

Once you have the VHD (which I placed at "C:\vhd\Win8-base.vhd"), you're ready to go! For myself, I'm going to create the first differencing VHD before proceeding. I have a tendency to really mess stuff up, and keeping that initial VHD absolutely pure gives me a nice retreat in case something goes wrong.

Creating a differencing VHD is not difficult, but it's not particularly easy, either. Just run the following [diskpart](http://technet.microsoft.com/en-us/library/cc770877) commands in an elevated command prompt (this can be done in Windows 7, too):

    > diskpart
    > create vdisk file=C:\vhd\Win8.vhd parent=C:\vhd\Win8-base.vhd
    > exit

It should only take a few seconds to create the differencing VHD, because all it really needs to do is reference the parent VHD.

## Step 4 - Backup BCD

OK, now we're to the point where we edit the BCD (Boot Configuration Data). This is where it gets scary.

So the first thing we do (of course) is backup our current BCD so that we can restore it later when... er... **if** we mess anything up.

Back to the elevated command prompt, using [bcdedit](http://technet.microsoft.com/en-us/library/cc731662.aspx) (again, Windows 7 can do this part):

    > bcdedit /export C:\vhd\bcdbackup

## Step 5 - Add Boot Entry

First, mount the VHD you want to boot to. If you created a differencing VHD in Step 3, then you want to use the child VHD ("Win8.vhd"), not the parent VHD ("Win8-base.vhd"). On Windows 8 you can just right-click the VHD and select Mount; on Windows 7 you have to go into the Disk Manager, select Actions -> Attach VHD, and browse to the VHD file (I mounted mine read-only).

Then (assuming that it was mounted at drive K:), run bcdboot from an elevated command prompt (this also works on Windows 7):

    > bcdboot k:\windows

Then you reboot. Crossing your fingers or saying a brief prayer would not hurt.

## Step 6 - Repeat as Necessary

You can repeat steps 3 (creating a differencing VHD from an existing VHD) and 5 (mounting the child VHD and adding a boot entry) as many times as you like.

You may also want to customize the boot menu, which can be done by using bcdedit (in this example, I list all of my boot options and then change the description of my old Win8 DevPreview from "Windows 8" to "Old Win8":

    > bcdedit
    
    Windows Boot Manager
    --------------------
    identifier              {bootmgr}
    device                  partition=\Device\HarddiskVolume1
    description             Windows Boot Manager
    locale                  en-US
    inherit                 {globalsettings}
    integrityservices       Enable
    default                 {current}
    resumeobject            {244905a5-985a-11de-8155-c187f01c6abe}
    displayorder            {current}
                            {244905a2-985a-11de-8155-c187f01c6abe}
                            {2449059d-985a-11de-8155-c187f01c6abe}
    toolsdisplayorder       {memdiag}
    timeout                 30
    
    Windows Boot Loader
    -------------------
    identifier              {current}
    device                  partition=C:
    path                    \windows\system32\winload.exe
    description             Windows 8
    locale                  en-US
    inherit                 {bootloadersettings}
    recoverysequence        {244905a7-985a-11de-8155-c187f01c6abe}
    integrityservices       Enable
    recoveryenabled         Yes
    allowedinmemorysettings 0x15000075
    osdevice                partition=C:
    systemroot              \windows
    resumeobject            {244905a5-985a-11de-8155-c187f01c6abe}
    nx                      OptIn
    bootmenupolicy          Standard
    
    Windows Boot Loader
    -------------------
    identifier              {244905a2-985a-11de-8155-c187f01c6abe}
    device                  vhd=[G:]\vhd\vs2012rc.vhd
    path                    \Windows\system32\winload.exe
    description             Windows 8
    locale                  en-US
    inherit                 {bootloadersettings}
    recoverysequence        {244905a3-985a-11de-8155-c187f01c6abe}
    integrityservices       Enable
    recoveryenabled         Yes
    allowedinmemorysettings 0x15000075
    osdevice                vhd=[G:]\vhd\vs2012rc.vhd
    systemroot              \Windows
    resumeobject            {244905a1-985a-11de-8155-c187f01c6abe}
    nx                      OptIn
    bootmenupolicy          Standard
    
    Windows Boot Loader
    -------------------
    identifier              {2449059d-985a-11de-8155-c187f01c6abe}
    device                  partition=G:
    path                    \Windows\system32\winload.exe
    description             Windows Server 2008 R2
    locale                  en-US
    inherit                 {bootloadersettings}
    recoverysequence        {2449059e-985a-11de-8155-c187f01c6abe}
    recoveryenabled         Yes
    osdevice                partition=G:
    systemroot              \Windows
    resumeobject            {2449059c-985a-11de-8155-c187f01c6abe}
    nx                      OptOut
    
    > bcdedit /set {244905a2-985a-11de-8155-c187f01c6abe} description "Old Win8"

Enjoy!

