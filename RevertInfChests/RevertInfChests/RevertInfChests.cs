using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrariaApi.Server;
using System.Reflection;
using TShockAPI;
using TShockAPI.DB;
using System.Threading.Tasks;
using Terraria;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;

namespace RevertInfChests
{
    [ApiVersion(1, 15)]
    public class RevertInfChests : TerrariaPlugin
    {
        #region Permission Strings
        private static string PRIMARY = "revinfchests.admin.revchest";
        #endregion

        #region Info
        public override string Name {
            get { return "RevertInfChests"; }
        }

        public override string Author {
            get { return "Fardiaz"; }
        }

        public override Version Version {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override string Description {
            get { return "Revert existing chest information converted with InfiniteChests plugin by MarioE to world file"; }
        }
        #endregion

        #region Members
        IDbConnection Database;
        #endregion

        public RevertInfChests(Main game)
            : base(game) {
            Order = 8888;
        }

        public override void Initialize() {
            Commands.ChatCommands.Add(new Command(PRIMARY, RevertChest, "revchest"));
            Commands.ChatCommands.Add(new Command(PRIMARY, RevertChests, "revchests"));

            /// Copied from https://github.com/ancientgods/AIO ----- I'm apologize AncientGods, I'm too lazy :)
            switch (TShock.Config.StorageType.ToLower()) {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    Database = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                host[0],
                                host.Length == 1 ? "3306" : host[1],
                                TShock.Config.MySqlDbName,
                                TShock.Config.MySqlUsername,
                                TShock.Config.MySqlPassword)
                    };
                    break;
                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "chests.sqlite");

                    if (File.Exists(sql)) {
                        Database = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    }
                    break;
            }
        }

        void RevertChest(CommandArgs e) {
            if (e.Parameters.Count != 2) {
                e.Player.SendErrorMessage("Invalid command! Proper syntax: /revchest <x> <y>");
                return;
            }

            Task.Factory.StartNew(() =>
            {
                int x = Convert.ToInt32(e.Parameters[0]);
                int y = Convert.ToInt32(e.Parameters[1]);

                using (var reader = Database.QueryReader(
                    "SELECT Items FROM Chests WHERE X = @0 AND Y = @1 AND WorldID = @2", x, y, Main.worldID)) {
                    while (reader.Read()) {
                        string items = reader.Get<string>("Items");

                        int id = Terraria.Chest.CreateChest(x, y);

                        if (Main.chest[id] != null) {
                            if (Main.chest[id].item == null)
                                Main.chest[id].item = new Item[Terraria.Chest.maxItems];

                            string[] split = items.Split(',');
                            for (int i = 0; i < 40; ++i) {
                                Item item = TShock.Utils.GetItemById(Convert.ToInt32(split[i * 3]));
                                item.stack = Convert.ToInt32(split[i * 3 + 1]);
                                item.prefix = Convert.ToByte(split[i * 3 + 2]);

                                Main.chest[id].item[i] = item;
                            }

                            e.Player.SendSuccessMessage("[InfiniteChests] Reverted chest {0},{1}",
                                Main.chest[id].x, Main.chest[id].y);
                        } else {
                            e.Player.SendErrorMessage("[InfiniteChests] Can not revert chest at {0},{1}",
                                Main.chest[id].x, Main.chest[id].y);
                        }
                    }
                }
                
                //if (count > 0)
                //    WorldFile.saveWorld();
            });
        }

        void RevertChests(CommandArgs e) {
            Task.Factory.StartNew(() =>
            {
                int count = 0;

                using (var reader = Database.QueryReader("SELECT X, Y, Items FROM Chests WHERE WorldID = @0",
                        Main.worldID)) {
                    while (reader.Read()) {
                        int x = reader.Get<int>("X");
                        int y = reader.Get<int>("Y");
                        string items = reader.Get<string>("Items");

                        int id = Terraria.Chest.CreateChest(x, y);

                        if (Main.chest[id] != null) {
                            if (Main.chest[id].item == null)
                                Main.chest[id].item = new Item[Terraria.Chest.maxItems];

                            string[] split = items.Split(',');
                            for (int i = 0; i < 40; ++i) {
                                Item item = TShock.Utils.GetItemById(Convert.ToInt32(split[i * 3]));
                                item.stack = Convert.ToInt32(split[i * 3 + 1]);
                                item.prefix = Convert.ToByte(split[i * 3 + 2]);

                                Main.chest[id].item[i] = item;
                            }

                            // e.Player.SendSuccessMessage("[InfiniteChests] Reverted chest {0},{1}",
                            //    Main.chest[id].x, Main.chest[id].y);

                            count++;
                        } else {
                            //e.Player.SendErrorMessage("[InfiniteChests] Can not revert chest at {0},{1}",
                            //    Main.chest[id].x, Main.chest[id].y);
                        }
                    }
                }

                e.Player.SendSuccessMessage("Reverted {0} chest{1}.", count, count == 1 ? "" : "s");

                //if (count > 0)
                //    WorldFile.saveWorld();
            });
        }
    }
}
