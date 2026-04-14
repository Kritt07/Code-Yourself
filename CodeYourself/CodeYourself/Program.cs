using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeYourself.Controllers;
using CodeYourself.Models;

namespace CodeYourself
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var model = new GameModel();
            var controller = new GameController(model);
            Application.Run(new GameForm(model, controller));
        }
    }
}
