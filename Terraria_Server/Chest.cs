using System;

namespace Terraria_Server
{
    public class Chest
    {
        public static int MAX_ITEMS = 20;
        public Item[] contents = new Item[MAX_ITEMS];
        public int x;
		public int y;

		public object Clone()
		{
			return base.MemberwiseClone();
		}

		public static void Unlock(int X, int Y)
		{
			for (int i = X; i <= X + 1; i++)
			{
				for (int j = Y; j <= Y + 1; j++)
				{
					if ((Main.tile.At(i, j).FrameX >= 72 && Main.tile.At(i, j).FrameX <= 106) || (Main.tile.At(i, j).FrameX >= 144 && Main.tile.At(i, j).FrameX <= 178))
					{
						var tile = Main.tile.At(i, j);
						tile.SetFrameX((short)(tile.FrameX - 36));
					}
				}
			}
		}

        public Chest()
        {
            for (int i = 0; i < contents.Length; i++)
            {
                this.contents[i] = new Item();
            }
        }

        public bool hasContents()
        {
            foreach (Item item in contents)
            {
                if (item.Type > 0 && item.Stack > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static int UsingChest(int i)
        {
			var chest = Main.chest[i];
			if (chest != null)
			{
				int index = 0;
				foreach(Player player in Main.players)
				{
					if (player.Active && player.chest == i)
					{
						if (Math.Abs (player.Position.X/16 - chest.x) < 7 && Math.Abs (player.Position.Y/16 - chest.y) < 7)
						{
							return index;
						}
						else
						{
							player.chest = -1;
						}
					}
					index++;
				}
			}
            return -1;
        }

        public static int FindChest(int X, int Y)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (Main.chest[i] != null && Main.chest[i].x == X && Main.chest[i].y == Y)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int CreateChest(int X, int Y)
        {
            //Opposite logic of find chest in that if the chest is found we now return -1.
            if (FindChest(X, Y) != -1)
            {
                return -1;
            }

            for (int i = 0; i < 1000; i++)
            {
                if (Main.chest[i] == null)
                {
                    Main.chest[i] = new Chest();
                    Main.chest[i].x = X;
                    Main.chest[i].y = Y;
                    for (int j = 0; j < Chest.MAX_ITEMS; j++)
                    {
                        Main.chest[i].contents[j] = new Item();
                    }
                    return i;
                }
            }
            return -1;
        }

        public static bool DestroyChest(int X, int Y)
        {
            int chestIndex = FindChest(X, Y);
            if (chestIndex == -1)
            {
                return true;
            }

            Chest chestToDestroy = Main.chest[chestIndex];
            if (chestToDestroy.hasContents())
            {
                return false;
            }

            Main.chest[chestIndex] = null;
            return true;
        }
    }
}
