---
layout: post
title: "Soyo widescreen monitor inf available"
---
This isn't exactly a programming-related post, but I had a problem that kept bugging me, so I finally decided to sit down and fix it today. I ended up writing a monitor .inf file for the Soyo Topaz 24"; this .inf file fixes two common problems:
1. Users cannot select the native 1920x1200 resolution
1. Games fail with a "Signal out of range" error

Like many other people, I got in on the nice OfficeMax Black Friday sale about a year ago, scooping up (among other things) a [Soyo Topaz S 24" widescreen monitor](http://www.soyo.com/product/LCD_Monitors/9/TOPAZ_S_-_24%26quot%3B_Wide_TFT_LCD_Monitor/408)... nice.

However, like many other people, I had problems with Windows recognizing the natural display resolution of 1920x1200. It turns out that this monitor does not correctly report its supported resolutions; furthermore, Soyo's tech support leaves quite a bit to be desired - they have yet to admit that there is a problem.

A lot of folks simply returned their monitors, but I went the route of a few others, disabling Windows' restrictions on resolutions. One obvious problem with this approach is that if you select a wrong resolution you can actually damage the hardware (at least, I know this used to be true, and the warning is still in the Windows dialog box). You just have to be careful not to select resolutions or refresh rates your monitor doesn't support.

I was happily using my monitor in this fashion until today, when I tried to install a DirectX game. Like many other games, it automatically attempted to raise the refresh rate - not realizing that the monitor is an LCD and not CRT. This resulted in the infamous "Signal out of range" monitor message. In fact, no matter what I tried, this would happen, because even restricting to the monitor's supported resolutions did not restrict the frame rate (bad, Soyo, bad!)

So, I decided to whip out the Soyo manual and make a custom monitor .inf file for my Soyo monitor... since they weren't going to do it. It took a bit more tinkering and time than I expected, but at the end of the day I was the proud owner of a monitor .inf file for the Soyo Topaz DYLM24D6.

I decided to release this little utility for free [on SourceForge](https://sourceforge.net/project/showfiles.php?group_id=213700&package_id=283420). This should work for every commonly-used Windows system (2000, XP, 2003, Vista, 2008 / x86, x64, IA-64), although I've only thoroughly tested it on Vista x64.

To install it, just right-click on the "Generic Non-PnP Monitor" in the Device Manager, update the drivers, and select the inf file.

[![]({{ site_url }}/assets/Step+1.png)]({{ site_url }}/assets/Step+1.png)

Right-click the "Generic Non-PnP Monitor" and select "Update Driver Software..."

[![]({{ site_url }}/assets/Step+2.png)]({{ site_url }}/assets/Step+2.png)

Choose "Browse my computer for driver software"

[![]({{ site_url }}/assets/Step+3.png)]({{ site_url }}/assets/Step+3.png)

Choose "Let me pick from a list of device drivers on my computer"

[![]({{ site_url }}/assets/Step+4.png)]({{ site_url }}/assets/Step+4.png)

Click "Have Disk..."

[![]({{ site_url }}/assets/Step+5.png)]({{ site_url }}/assets/Step+5.png)

Click "Browse..."

[![]({{ site_url }}/assets/Step+6.png)]({{ site_url }}/assets/Step+6.png)

Select the Soyo.inf that was downloaded from SourceForge.net

[![]({{ site_url }}/assets/Step+7.png)]({{ site_url }}/assets/Step+7.png)

Click "Next"

[![]({{ site_url }}/assets/Step+8.png)]({{ site_url }}/assets/Step+8.png)

Confirm security question

Enjoy!