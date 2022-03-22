---
layout: post
title: "Dealing with WiX data files"
---
I am not an installer guru. The story below is how another company overcame one of their installer upgrade difficulties. The solution was found by their installer guru, a friend of mine.

Splitting up an application into components is a pretty [straightforward process](http://msdn.microsoft.com/en-us/library/aa368269(VS.85).aspx?WT.mc_id=DT-MVP-5000058) - usually, resource files are thrown into a directory-wide component. Apparently, the ideal setup for ".config" files is to be in the same component as their .exe, with their CompanionFile set to the .exe, like this: [http://wix.mindcapers.com/wiki/Companion_File](http://wix.mindcapers.com/wiki/Companion_File).

That's nice. Now, what to do if your previous installs didn't do this?

Unfortunately, our situation was even worse. We had the .config file being installed as its own component, with a util:XmlFile modifying the file at the end of the install. This has the unfortunate side effect of the installer sometimes not wanting to update this file (since it's an XML file, it has no version information, and the modification date may be newer since it was modified at install time by the previous installer).

(BTW, for other users of util:XmlFile, there is an attribute PreserveModifiedDate. If our previous installer had set this to "yes", then we wouldn't have had these problems. But it didn't, so the modified date is changed, and we ended up where we were today.)

The solution we adopted is called "version lying". We added a DefaultVersion attribute to the File element to force the new installer to overwrite the old file. Of course, if the end user had changed the .config file after installing, then the new install would blow that away.

WiX doesn't really like version lying a lot: it will give you a warning. However, it works. We are using an environment variable for the build version, and we just set DefaultVersion to "$(env.MY_BUILD_VERSION)". This way the fake version will stay in sync with the final build version.

For our next major update, we're going to use PreserveModifiedDate and either not support automatic upgrades (forcing the user to uninstall the old version first) or upgrade to a different directory. We can then drop the version lying, and our installers will be kosher from then on.

