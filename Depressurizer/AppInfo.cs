﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Depressurizer {
    [Flags]
    enum AppPlatforms {
        None = 0,
        Windows = 1,
        Mac = 1 << 1,
        Linux = 1 << 2,
        All = Windows | Mac | Linux
    }

    enum AppType2 {
        Application,
        Demo,
        DLC,
        Game,
        Media,
        Tool,
        Other,
        Unknown
    }

    class AppInfo {
        public int appId;
        public string name;
        public AppType2 type;

        public AppPlatforms platforms;

        public AppInfo( int id, string name = null, AppType2 type = AppType2.Unknown, AppPlatforms platforms = AppPlatforms.All ) {
            this.appId = id;
            this.name = name;
            this.type = type;

            this.platforms = platforms;
        }

        public static AppInfo FromVdfNode( VdfFileNode commonNode ) {
            if( commonNode == null || commonNode.NodeType != ValueType.Array ) return null;

            AppInfo result = null;

            VdfFileNode idNode = commonNode.GetNodeAt( new string[] { "gameid" }, false );
            int id = -1;
            if( idNode != null ) {
                if( idNode.NodeType == ValueType.Int ) {
                    id = idNode.NodeInt;
                } else if( idNode.NodeType == ValueType.String ) {
                    if( !int.TryParse( idNode.NodeString, out id ) ) {
                        id = -1;
                    }
                }
            }


            if( id >= 0 ) {
                // Get name
                string name = null;
                VdfFileNode nameNode = commonNode.GetNodeAt( new string[] { "name" }, false );
                if( nameNode != null ) name = nameNode.NodeData.ToString();

                // Get type
                string typeStr = null;
                AppType2 type = AppType2.Unknown;
                VdfFileNode typeNode = commonNode.GetNodeAt( new string[] { "type" }, false );
                if( typeNode != null ) typeStr = typeNode.NodeData.ToString();

                if( typeStr != null ) {
                    if( !Enum.TryParse<AppType2>( typeStr, true, out type ) ) {
                        type = AppType2.Other;
                    }
                }

                // Get platforms
                string oslist = null;
                AppPlatforms platforms = AppPlatforms.All;
                VdfFileNode oslistNode = commonNode.GetNodeAt( new string[] { "oslist" }, false );
                if( oslistNode != null ) {
                    oslist = oslistNode.NodeData.ToString();
                    if( oslist.IndexOf( "windows", StringComparison.OrdinalIgnoreCase ) != -1 ) {
                        platforms |= AppPlatforms.Windows;
                    }
                    if( oslist.IndexOf( "mac", StringComparison.OrdinalIgnoreCase ) != -1 ) {
                        platforms |= AppPlatforms.Mac;
                    }
                    if( oslist.IndexOf( "linux", StringComparison.OrdinalIgnoreCase ) != -1 ) {
                        platforms |= AppPlatforms.Linux;
                    }
                }

                result = new AppInfo( id, name, type, platforms );

            }
            return result;
        }

        public static Dictionary<int, AppInfo> LoadApps( string path ) {
            Dictionary<int, AppInfo> result = new Dictionary<int, AppInfo>();
            BinaryReader bReader = new BinaryReader( new FileStream( path, FileMode.Open, FileAccess.Read ) );
            long fileLength = bReader.BaseStream.Length;

            // seek to common: start of a new entry
            byte[] start = new byte[] {0x02, 0x00, 0x63, 0x6F, 0x6D, 0x6D, 0x6F, 0x6E, 0x00}; // 0x02 0x00 c o m m o n 0x00

            VdfFileNode.ReadBin_SeekTo( bReader, start, fileLength );

            VdfFileNode node = VdfFileNode.LoadFromBinary( bReader, fileLength );
            while( node != null ) {
                AppInfo app = AppInfo.FromVdfNode( node );
                if( app != null ) {
                    result.Add( app.appId, app );
                }
                VdfFileNode.ReadBin_SeekTo( bReader, start, fileLength );    
                node = VdfFileNode.LoadFromBinary( bReader, fileLength );
            }
            bReader.Close();
            return result;
        }
    }
}
