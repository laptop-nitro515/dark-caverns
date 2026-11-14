// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

namespace DarkCaverns.Core.World
{
    /// <summary>The six stores of Moria's town, numbered as in Moria.</summary>
    public enum Shop
    {
        GeneralStore = 1,
        Armoury = 2,
        Weaponsmith = 3,
        Temple = 4,
        Alchemist = 5,
        MagicShop = 6,
    }

    public static class ShopInfo
    {
        public static string Name(this Shop shop)
        {
            switch (shop)
            {
                case Shop.GeneralStore: return "General Store";
                case Shop.Armoury: return "Armoury";
                case Shop.Weaponsmith: return "Weaponsmith";
                case Shop.Temple: return "Temple";
                case Shop.Alchemist: return "Alchemy Shop";
                default: return "Magic Shop";
            }
        }
    }
}
