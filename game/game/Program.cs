﻿using game.Controllers;
using game.Managers;
using System.Drawing;

public class Program
{
    public static void Main()
    {
        Game gm = new Game();
        gm.Init();
        gm.Run();
    }
}