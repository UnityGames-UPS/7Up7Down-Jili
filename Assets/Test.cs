using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //  CountCharacter();
        //  CountWord();
        //  pattern1();
       // Pyramid();
       // InversePyramid();
        Pyramid1();
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    void CountCharacter
    ()
    {
        string str = "HelloBro";
        Dictionary<char, int> dict = new Dictionary<char, int>();
        foreach (char c in str)
        {
            if (dict.ContainsKey(c))
            {
                dict[c]++;
            }
            else
            {
                dict[c] = 1;
            }
        }

        foreach (var a in dict)
        {
            Debug.Log("dict: " + a.Key + ": " + a.Value);
        }
    }

    void CountWord()
    {
        string str = "Hii Bro what are you doing hii";
        string[] words = str.ToLower().Split(' ');
        Dictionary<string, int> dict = new Dictionary<string, int>();

        foreach (string word in words)
        {
            if (dict.ContainsKey(word))
            {
                dict[word]++;
            }
            else
            {
                dict[word] = 1;
            }
        }
        foreach (var a in dict)
        {
            Debug.Log("dict: " + a.Key + ": " + a.Value);
        }
    }

    void pattern1()
    {
        int row = 5;
        for (int i = 0; i < row; i++)
        {
            string str = " ";
            for (int j = 0; j <= i; j++)
            {
                str += "*";
            }
            Debug.Log(str);
        }
    }

    void Pyramid()
    {
        int row = 5;
        for (int i = 1; i <= row; i++)
        {
            string str = "";
            for (int j = i; j <= row; j++)
            {
                str += " ";
            }
            for (int k = 1; k <= ((2 * i) - 1); k++)
            {
                str += "*";
            }
            Debug.Log(str);
        }
    }

   void InversePyramid()
{
    int row = 5;
    for(int i=row; i>=1; i--)
    {
        string str = "";

        // Spaces
        for (int j = 0; j < row - i; j++)
            str += " ";

        // Stars
        for (int k = 1; k <= 2*i - 1; k++)
            str += "*";

            Debug.Log(str);

            int[] arr = { 1, 2, 2, 3 };
            Array.Reverse(arr); 
    }
}


void Pyramid1()
{
    int row=5;
    string str="";
    for(int i=1;i<=5;i++)
    {
        
        for(int j=i;j<=row;j++)
        {
            str+=" ";
        }
        for(int k=1;k<=((2*i)-1);k++)
        {
            str+="*";
        }
        str+="\n";
        
    }
    Debug.Log(str);

    //  for (int i = 1; i <= row; i++)
    //     {
    //         string str = "";
    //         for (int j = i; j <= row; j++)
    //         {
    //             str += " ";
    //         }
    //         for (int k = 1; k <= ((2 * i) - 1); k++)
    //         {
    //             str += "*";
    //         }
    //         Debug.Log(str);
    //     }
}
    
}
