$dir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

$env:PATH += ";" + $dir + "\ruby\bin;" + $dir + "\devkit\bin;" + $dir + "\git\bin;" + $dir + "\Python\App;" + $dir + "\Python\App\Scripts;" + $dir + "\devkit\mingw\bin;" + $dir + "\curl\bin"
$env:SSL_CERT_FILE = $dir + "\curl\bin\cacert.pem"