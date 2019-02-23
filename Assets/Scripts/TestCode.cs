using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCode : MonoBehaviour
{
    public int minDeadCount = 1;
    public int maxDeadCount = 4;
    public int minBirthCount = 3;
    public int maxBirthCount = 3;
    public int count;
    public int isAlive;
    
    private void Start()
    {
        Debug.Log(TestSyori(count, isAlive));
    }
    public int TestSyori(int count,int isAlive)
    {
        var result = 0;
        if (isAlive == 1)
        {//生きてる時は、死ぬかどうかの判定
            //var boolAlive = (count > minDeadCount) && (count < maxDeadCount);
            //result = boolAlive ? 1 : 0;
            if(count == 2 || count == 3)
            {
                result = 1;
            }
            else
            {
                result = 0;
            }
        }
        else
        {//死んでる時は、生まれるかどうかの判定
            var boolAlive = (count >= minBirthCount) && (count <= maxBirthCount);
            result = boolAlive ? 1 : 0;
        }
        return result;
    }
}
