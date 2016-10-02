# To build:

- Install PortableJekyll from http://www.madhur.co.in/blog/2013/07/20/buildportablejekyll.html
- Copy "Python\App\python.exe" to "Python\App\python2.exe"
- Copy setpath.ps1 to the directory where you uninstalled that zip (e.g., D:\Programs\PortableJekyll)
- Run "gem install jekyll"
- Download ez\_setup.py from https://pypi.python.org/pypi/setuptools#windows and run "python ez_setup.py".
- Run "easy_install Pygments"

To serve locally (with future posts):
> D:\Programs\PortableJekyll\setpath.ps1
> .\serve.ps1

To build:
> D:\Programs\PortableJekyll\setpath.ps1
> .\build.ps1

# Patterns:

Link to other posts: `[text to be highlighted]({% post_url 2016-01-01-post-file-name %})`

