using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PWR;
using PWR.Models;

namespace ExampleApp;

class Program
{
    static async Task Main()
    {
        await Vidas.Run();
        // await App.Run();
    }
}