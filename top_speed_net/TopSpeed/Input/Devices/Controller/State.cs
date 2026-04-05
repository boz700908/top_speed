namespace TopSpeed.Input.Devices.Controller
{
    internal struct State
    {
        public int X;
        public int Y;
        public int Z;
        public int Rx;
        public int Ry;
        public int Rz;
        public int Slider1;
        public int Slider2;
        public bool B1;
        public bool B2;
        public bool B3;
        public bool B4;
        public bool B5;
        public bool B6;
        public bool B7;
        public bool B8;
        public bool B9;
        public bool B10;
        public bool B11;
        public bool B12;
        public bool B13;
        public bool B14;
        public bool B15;
        public bool B16;
        public bool Pov1;
        public bool Pov2;
        public bool Pov3;
        public bool Pov4;
        public bool Pov5;
        public bool Pov6;
        public bool Pov7;
        public bool Pov8;

        public bool HasAnyButtonDown()
        {
            return B1 || B2 || B3 || B4 || B5 || B6 || B7 || B8 || B9 || B10 ||
                   B11 || B12 || B13 || B14 || B15 || B16 ||
                   Pov1 || Pov2 || Pov3 || Pov4 || Pov5 || Pov6 || Pov7 || Pov8;
        }

    }
}

