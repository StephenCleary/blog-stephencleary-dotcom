---
layout: post
title: "Sample Code: Getting the Local IP Addresses"
series: "TCP/IP .NET Sockets FAQ"
seriesOrder: 920
seriesTitle: "Local IP Addresses Example"
---
The sample code below enumerates all the adapters on a machine, and then enumerates all IPv4 addresses for each adapter. This is necessary because [a computer may have multiple IP addresses]({% post_url 2009-05-18-getting-local-ip-address %}).

 
{% highlight csharp %}
/// <summary>
/// This utility function displays all the IP (v4, not v6) addresses of the local computer.
/// </summary>
public static void DisplayIPAddresses()
{
    StringBuilder sb = new StringBuilder();
  
    // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
    NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
  
    foreach (NetworkInterface network in networkInterfaces)
    {
        // Read the IP configuration for each network
        IPInterfaceProperties properties = network.GetIPProperties();
  
        // Each network interface may have multiple IP addresses
        foreach (IPAddressInformation address in properties.UnicastAddresses)
        {
            // We're only interested in IPv4 addresses for now
            if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                continue;
  
            // Ignore loopback addresses (e.g., 127.0.0.1)
            if (IPAddress.IsLoopback(address.Address))
                continue;
  
            sb.AppendLine(address.Address.ToString() + " (" + network.Name + ")");
        }
    }
  
    MessageBox.Show(sb.ToString());
}
{% endhighlight %}
