using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TextIO : MonoBehaviour
{
    public static string input = "";
    public static string output = "";
    private Thread inputLoop;
    private Thread frotzLoop;
    public Text gt;

    public void FrotzLoop()
    {
        print("Begin Frotzing!");

        string[] string_list = new string[] { "ZORK1.dat" };
        ReadOnlySpan<string> string_span = new ReadOnlySpan<string>(string_list);

        Frotz.Generic.Main.TestFunc(string_span);
        Frotz.Generic.Main.MainFunc(string_span);
    }

    public void InputLoop()
    {
        while (true)
        {
            output += input;
            input = "";
            Thread.Sleep(10);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        UnitySystemConsoleRedirector.Redirect();

        gt = GetComponent<Text>();
        gt.text = "Enter some text.\n\n";

        //inputLoop = new Thread(new ThreadStart(InputLoop));
        //inputLoop.Start();

        frotzLoop = new Thread(new ThreadStart(FrotzLoop));
        frotzLoop.Start();

    }

    // Update is called once per frame
    void Update()
    {
        foreach (char i in Input.inputString)
        {
            char c = i;
            if (c == '\r')
            {
                c = '\n';
            }
            if (c == '\n')
            {
                gt.text = "";
            }
            input += c;
            output += c;
        }

        gt.text += output;
        output = "";

    }

}
