using SilkBound;
using MelonLoader;
using System;
using UnityEngine;
using SilkBound.Types;
using SilkBound.Utils;


//Current Problem: 



[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
namespace SilkBound
{
    public class ModMain : MelonMod
    {
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.H)) {
                Server.ConnectPiped("sb_dbg", "server");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                NetworkUtils.Connect(new NamedPipeConnection("sb_dbg"), "client");
            }
        }
    }
}
