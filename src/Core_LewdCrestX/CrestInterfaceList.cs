using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_LewdCrestX
{
    public sealed class CrestInterfaceList
    {
        public static CrestInterfaceList Create(bool onlyImplemented, bool separateImplemented)
        {
            IEnumerable<CrestInfo> infos = LewdCrestXPlugin.CrestInfos.Values.OrderByDescending(x => separateImplemented && x.Implemented).ThenBy(x => x.Name);
            if (onlyImplemented) infos = infos.Where(x => x.Implemented);
            var crestInfos = infos.ToList();
            var list = new CrestInterfaceList();
            list._interfaceCrestTypes = new[] { CrestType.None }.Concat(crestInfos.Select(x => x.Id)).ToArray();
            list._interfaceCrestNames = new[] { "No crest" }.Concat(crestInfos.Select(x => !onlyImplemented && separateImplemented && x.Implemented ? "[+] " + x.Name : x.Name)).ToArray();
            crestInfos.Insert(0, null);
            list._interfaceCrestInfos = crestInfos.ToArray();
            return list;
        }

        public int GetIndex(CrestType type) => Mathf.Max(0, Array.IndexOf(_interfaceCrestTypes, type));
        public CrestType GetType(int index) => index <= 0 || index >= _interfaceCrestNames.Length ? CrestType.None : _interfaceCrestTypes[index];
        public CrestInfo GetInfo(CrestType type) => _interfaceCrestInfos[Mathf.Max(0, Array.IndexOf(_interfaceCrestTypes, type))];
        public CrestInfo GetInfo(int index) => index <= 0 || index >= _interfaceCrestInfos.Length ? null : _interfaceCrestInfos[index];
        public string[] GetInterfaceNames() => _interfaceCrestNames;

        private CrestInfo[] _interfaceCrestInfos;
        private string[] _interfaceCrestNames;
        private CrestType[] _interfaceCrestTypes;
    }
}