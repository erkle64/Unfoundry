﻿using System.Collections.Generic;
using System.Text;

namespace Unfoundry
{
    public class CustomRadialMenuStateControl
    {
        private CustomRadialMenuOption[] menuOptions;
        private Dictionary<ulong, CustomRadialMenuOption[]> menuOptionsByMask = new Dictionary<ulong, CustomRadialMenuOption[]>();

        private ulong BitsAll => (1ul << menuOptions.Length) - 1ul;

        public CustomRadialMenuStateControl(params CustomRadialMenuOption[] menuOptions)
        {
            this.menuOptions = new CustomRadialMenuOption[menuOptions.Length];
            menuOptions.CopyTo(this.menuOptions, 0);
        }

        public CustomRadialMenuOption[] GetMenuOptions(ulong mask)
        {
            CustomRadialMenuOption[] menuOptionsOut;
            if (menuOptionsByMask.TryGetValue(mask, out menuOptionsOut)) return menuOptionsOut;

            menuOptionsOut = new CustomRadialMenuOption[NumberOfSetBits(mask)];
            var bit = 1ul;
            for(int i = 0, j = 0; i < menuOptions.Length; ++i)
            {
                if ((mask & bit) != 0)
                {
                    menuOptionsOut[j++] = menuOptions[i];
                }
                bit <<= 1;
            }

            menuOptionsByMask[mask] = menuOptionsOut;
            return menuOptionsOut;
        }

        public CustomRadialMenuOption[] GetMenuOptions(params bool[] states)
        {
            var mask = 0ul;
            var bit = 1ul;
            foreach (var state in states)
            {
                if(state) mask |= bit;
                bit <<= 1;
            }

            return GetMenuOptions(mask);
        }

        public CustomRadialMenuOption[] GetMenuOptions()
        {
            var mask = 0ul;
            var bit = 1ul;
            foreach (var menuOption in menuOptions)
            {
                if (menuOption.IsEnabled) mask |= bit;
                bit <<= 1;
            }

            return GetMenuOptions(mask);
        }

        static int NumberOfSetBits(ulong i)
        {
            i = i - ((i >> 1) & 0x5555555555555555UL);
            i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }

        static string UlongToString(ulong value)
        {
            int ulongsize = sizeof(ulong) * 8;
            StringBuilder ret = new StringBuilder(ulongsize);

            for (int i = 0; i < ulongsize; i++)
            {
                ret.Insert(0, (value & 0x1).ToString());
                value >>= 1;
                if (value == 0) break;
            }

            return ret.ToString();
        }
    }
}
