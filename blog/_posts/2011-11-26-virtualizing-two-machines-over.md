---
layout: post
title: "Virtualizing Two Machines over Thanksgiving"
tags: ["IT"]
---


Like many "computer people," I do a lot of admin work for friends and familiy. Over the last few years, I've worked with [my church](http://landmarkbaptist.com) to get them out of the dark ages of computing. The process is almost complete; I only have one more machine to replace, and then they will all be 64-bit dual-core 4GB systems running Pro editions of Windows. Next year I hope to (finally) put in a domain.





It turns out that two of the old machines have some outdated software that's critical to weekly operations. I'm working on replacements for the software, but in the meantime, the old machines were just sitting around, taking up space in the church office.





I decided to try to virtualize these machines on Friday (the day after Thanksgiving). This blog entry is just a "lessons learned" from this adventure.



## The Challenge, and the Plan



The old "server" (XP Home with 192 MB RAM) and the old office machine (XP Home with 256 MB RAM) both needed virtualization. Due to the way the weekly process is done in the office, the old server would have to be virtualized onto the new server, and the old office machine would have to be virtualized onto the new office machine.





I'm most familiar with [VMWare products](http://www.vmware.com/) (particularly VMWare Workstation), and I highly recommend them. However, I wanted to see if it was possible to virtualize these machines without incurring a licensing cost. My budget at Landmark Baptist isn't comparable to most IT departments. ;)  So, I decided to try Hyper-V or Virtual PC, falling back on VirtualBox if necessary (it wasn't).





The server was the first machine to be replaced, so unfortunately at this point the new server has the most outdated hardware/OS. It's running Server 2008 but without Hyper-V... or even CPU virtualization support. :(  Furthermore, according to what I've read, Hyper-V doesn't support USB, which IMO is a significant limitation (and a showstopper for the old "server").





So, I decided to try using Virtual PC for both virtual machines. The new office machine runs Win7 Pro, which is fully supported by the current version of Virtual PC ("Windows Virtual PC"). I was a bit apprehensive about the new server; Server 2008 isn't an officially supported platform for the previous version of Virtual PC ("Microsoft Virtual PC 2007 SP1"), but it turned out to work fine. Microsoft still has Virtual PC 2007 available for download, and SP1 added support for machines without virtualization hardware (which is just what I needed).





One limitation with Virtual PC is that it can only handle 127 GB hard drives. In my case, both machines had hard drives much smaller than that, so it wasn't a problem.





The plan at this point was to virtualize each machine to a different version of Virtual PC (running on different OSes and hardware). We'll see how well this worked in a moment, but first I'll mention the tool which kicked off this whole adventure.





Systems Internals has a great tool called [disk2vhd](http://technet.microsoft.com/en-us/sysinternals/ee656415), which can create a virtual disk from a physical disk - even storing the virtual disk image on the physical disk it's imaging, while the physical disk is running the OS running disk2vhd. If you think about it, that's pretty cool.





Disk2vhd can take quite a while (i.e., 8-10 hours) to run, so I tried to make my plan where it would run overnight. Once I have the machines in a VHD image, I should be able to create a Virtual PC machine using that for a hard drive. VirtualBox also supports VHD, so my fallback would be ready just in case.





There are several articles on the Internet where others have successfully converted a physical XP machine to a virtual PC on Windows 7. The steps are straightforward: Create a disk image using disk2vhd; copy the image to the host PC; set up a new virtual machine in Virtual PC; re-activate Windows on the virtual machine; and install Integration Components/Services.





One final note: during my preparations, I discovered that XP can run into a stop 0x7B when backing up to a disk image and restoring on different hardware (which is very similar to what I'm doing with disk2vhd). The steps to fix this are in [KB314082](http://support.microsoft.com/kb/314082). I did not run into this issue, but I'm including it here for others who may.





On Wednesday (the day before Thanksgiving), I had done all the research and established my plan. I downloaded disk2vhd, VirtualBox, and both versions of Virtual PC onto my USB drive and left for Petoskey. That night, I started both machines running disk2vhd and went over to my Mom's for Thanksgiving.



## A Snag: OEM OS



I popped in to check the status on Thursday morning. The server disk2vhd failed; my external USB drive had a faulty power adapter and it had shorted out overnight. So I restarted it with my other USB drive, and turned my attention to the office machine.





I had noticed on the disk2vhd download page that OEM OS licenses prevent virtualization. Turns out the office machine was XP Home OEM. The VHD came out fine, but it was not possible to re-activate Windows on the virtual machine. I did have a spare XP Home Retail key, but apparently you can't activate an OEM install with a Retail key. I also tried the original OEM key, but that didn't work since it's keyed to the BIOS which is different in a virtual machine.





Re-installing the OS was out of the question (if I actually _had_ the install media for the outdated programs, I would have installed them on the new machine and we wouldn't need to virtualize in the first place). In desperation, I searched online for any way to convert OEM to Retail in-place. Most of the articles recommended running a repair from a different CD, but that seemed hokey to me (how would that affect updates already installed?).





Finally, I discovered the [Product Key Update Tool](http://go.microsoft.com/fwlink/?LinkId=204141). I ran it on the old office machine, converting it from OEM to Retail, and then re-started disk2vhd. This time, I ran disk2vhd with the output disk image going directly over the network to the new host PC; this worked just fine and I highly recommend it.





During my searching, I also discovered **sysprep**. The Product Key Update Tool changes the old key to a new key; whereas Sysprep removes the existing key, requiring the user to type it in the next time the computer boots. I used the Update Tool, but Sysprep would probably also work.



## Another Snag: Remote Control



I was hoping to do most of the work on Friday from the comfort of my Mom's living room, eating Thanksgiving leftovers and watching the kids play with their uncles. Unfortunately, I could not get mouse capture to work at all remotely before Integration Services were installed.





It doesn't appear to be possible to set up a new virtual machine remotely. At least not using [LogMeIn](https://secure.logmein.com/), which is my remote control software of choice; in the past I've used pcAnywhere, UltraVNC, and Windows Live Mesh, but I've now settled solidly on LogMeIn.





I also tried to LogMeIn into another computer and Remote Desktop to the Virtual PC host; however, the mouse capture was still funky (the scale was messed up). Once I got Integration Services installed, remotely controlling a host PC worked fine.





So, I ended up having to physically be present for the initial virtual machine setup, which was disappointing.



## Another Snag: Networking



When I brought up the old "server" as a virtual machine on the new server, the networking didn't work. Since I only had the Windows Activation UI available, it wasn't possible to diagnose. By default, Virtual PC will share the host's network card (using a network switch in software). The new server had a static IP, but this shouldn't have caused a problem. When I switched it to use NAT (a network router in software), the problem went away.





I've always used NAT for my VMWare virtual machines, so this was a natural step.



## Issue: Slow Initial Boot



The "server" disk2vhd process never finished. I'm not entirely sure why; the disk file was approximately the correct size, but disk2vhd never completed. Eventually I just exited the program and decided to try to use the file anyway.





When starting the old server as a virtual machine for the first time, it took about an hour to get from initial startup, through Windows activation, and to the desktop. Virtual PC was pegging the CPU the entire time. I'm unsure of the reason for this; the host PC does not have virtualization hardware, the vhd could be incomplete, the vhd is dynamic, ...





Once I installed Integration Components and rebooted, the CPU problems disappeared. I can't say whether the resolution was due to the installation or the rebooting.



## Issue: Integration Components on XP Home



The virtualized XP office machine is running on Windows Virtual PC under a Windows 7 Pro host. Normally, this situation allows a really neat trick: you can set up a program on the virtual machine so it looks like a program on the host, with its own Start menu entry, running in a regular window instead of a full virtual machine desktop, etc.





Unfortunately, that does not work if the virtual machine is XP Home. Apparently, the Integration Components use RDP (Remote Desktop) for that functionality. The auto-login feature is also not available.



## Conclusion



The project was completed, though it took longer than I expected. I'll find out next week if everything works sufficiently on the virtualized machines.





Lessons learned:



- You cannot virtualize an OEM install. You have to change it to a Retail install first, using the [Product Key Update Tool](http://go.microsoft.com/fwlink/?LinkId=204141).
- [Disk2vhd](http://technet.microsoft.com/en-us/sysinternals/ee656415) can target a vhd image over the network.
- You must be physically present to set up the virtual machines, at least until the point that Integration Services are installed.
- If you're having problems getting the virtual machine on the network, try using NAT.
- Some Integration Components features do not work if the guest is XP Home.
