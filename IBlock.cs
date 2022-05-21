namespace Tetris
{
    public class IBlock : Block
    {
        private readonly Position[][] tiles = new Position[][]
        {
            new Position[] { new Position(1,0), new Position(1,1), new Position(1,2), new Position(1,3) },
            new Position[] { new Position(0,2), new Position(1,2), new Position(2,2), new Position(3,2) },
            new Position[] { new Position(2,0), new Position(2,1), new Position(2,2), new Position(2,3) },
            new Position[] { new Position(0,1), new Position(1,1), new Position(2,1), new Position(3,1) }
        };

        public override int Id => 1;
        protected override Position StartOffset => new Position(-1, 3);
        protected override Position[][] Tiles => tiles;
    }
}
