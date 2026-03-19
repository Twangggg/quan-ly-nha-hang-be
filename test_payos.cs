using System;
using System.Reflection;
using PayOS.Models.V2.PaymentRequests;

class Program
{
    static void Main()
    {
        var type = typeof(CreatePaymentLinkRequest);
        foreach (var prop in type.GetProperties())
        {
            Console.WriteLine(prop.Name + " - " + prop.PropertyType.Name);
        }
    }
}
