To use:

- Install PortableJekyll from http://www.madhur.co.in/blog/2013/07/20/buildportablejekyll.html
- Copy setpath.ps1 to the directory where you uninstalled that zip (e.g., D:\Programs\PortableJekyll)
- Run "gem install jekyll"
- Download ez\_setup.py from https://pypi.python.org/pypi/setuptools#windows and run "python ez_setup.py".
- Run "easy_install Pygments"

You can serve Jekyll from within VS, but it can't be stopped in the Package Manager Console. So use a regular Powershell console instead:
> D:\Programs\PortableJekyll\setpath.ps1
> jekyll serve -w

When publishing:
> jekyll serve -w --lsi
