using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandProjectPV_425.ViewModels
{
    public class AboutWindowViewModel
    {
        public string Title => "О программе Benchmark Analyzer"; 
        public string Description => "Приложение для измерения производительности численных задач с помощью BenchmarkDotNet."; // пока так
        public string Author => "Разработано командой BV425 (2025)"; // оставлю
    }
}