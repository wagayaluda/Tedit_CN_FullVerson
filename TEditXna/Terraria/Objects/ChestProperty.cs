﻿using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEdit.Geometry.Primitives;
using GalaSoft.MvvmLight;

namespace TEditXNA.Terraria.Objects
{
    public class ChestProperty : ObservableObject, ITile
    {
        public ChestProperty()
        {
            Name = "UNKNOWN";
            Id = 0;
        }

        private int _id;
        private string _name;
        private Vector2Short _size;
        private int _chestId;
        private Vector2Short _uV;
        private ushort _tileType;

        public int ChestId
        {
            get { return _chestId; }
            set { Set("ChestId", ref _chestId, value); }
        }

        public Vector2Short UV
        {
            get { return _uV; }
            set { Set("UV", ref _uV, value); }
        }

        public ushort TileType
        {
            get { return _tileType; }
            set { Set("TileType", ref _tileType, value); }
        }

        public Vector2Short Size
        {
            get { return _size; }
            set { Set("Size", ref _size, value); }
        } 

        public string Name
        {
            get { return _name; }
            set { Set("Name", ref _name, value); }
        }

        private WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { Set("Image", ref _image, value); }
        }

        public Color Color
        {
            get { return Colors.Transparent; }
        }

        public int Id
        {
            get { return _id; }
            set { Set("Id", ref _id, value); }
        }

    }
}
