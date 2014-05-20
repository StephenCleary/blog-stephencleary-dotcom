---
layout: post
title: "Software-only device drivers: What's in a name?"
---
There's currently no consensus on the terminology used to refer to device drivers that do not have hardware. These types of drivers are quite useful in many scenarios:

- Virtual CD-ROM drives can mount a CD image from your hard drive and pretend it was put in a CD-ROM drive
- Virtual network cards are used by somme VPN products like OpenVPN (as well as virtual machine systems like VMWare Workstation) to enable special netowrk communications (e.g., over a VPN or to a virtual machine).
- Virtual keyboards and mice are often used by gamepad systems
- Virtual serial ports are used for testing or running serial programs on laptops
- Virtual hard drives can be used as encrypted volumes
- Other virtual devices are often developed by companies for testing purposes

The examples above are all drivers for virtual hardware. There is another class of drivers without hardware: monitor drivers, which attach to drivers for real (or virtual) hardware and observe (and/or change) the data going in and out of that driver. This is how programs like FileSpy and Process Monitor work. Finally, some drivers simply do not have anything to do with any hardware at all, real or virtual.

There have been a few different names tossed around to describe these drivers without hardware:

 - "Virtual device drivers" - unfortunately, this has another meaning in the Windows world. Virtual device drivers (VxD's) were used in the 9x systems to help DOS programs run by sharing hardware - a virtual device was presented to each DOS program, which believed it had full access to the deivce, and the VxD would take care of managing the sharing of the device. Driver developers generall agree that the straightforward "virtual device driver" should not be used for drivers without hardware because of the possibility of this confusion.
 - "Software device drivers" - this term could refer to any device driver, because they are all software. The term "software device driver" is in fact regularly used in this fashion, especially by those who work more with hardware.

This leaves us with the unambiguous but rather unweildly terms "device driver without hardware", "hardwareless device driver", or "software-only device driver". Of these, the term "software-only device driver" seems to be getting some gradual [acceptance](http://www.google.com/search?q=%22software-only%22+device+driver) by the driver-writing community.

