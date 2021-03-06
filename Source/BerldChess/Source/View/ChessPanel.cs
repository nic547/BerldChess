﻿using BerldChess.Properties;
using ChessDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BerldChess.View
{
    public class ChessPanel : Panel
    {
        #region Fields

        private bool _displayCoordinates = true;
        private bool _renderImages = true;
        private int _boardDimension;
        private double _fieldSize;
        private double? _fontSizeFactor = null;
        private string _pieceFontFamily = string.Empty;
        private Color _darkSquare = Color.Brown;
        private Color _lightSquare = Color.WhiteSmoke;
        private Color _borderColor = Color.White;
        private Brush _legalMoveCircleBrush = new SolidBrush(Color.FromArgb(200, 99, 194, 107));
        private Point _boardLocation;
        private Point _selectedIndex = new Point(-1, -1);
        private Point _movingPieceIndex = new Point(-1, -1);
        private Point _movingPoint = new Point(-1, -1);
        private ChessGame _game = null;
        private ChessPiece[][] _board;
        private Bitmap[] _scaledPieceImages = new Bitmap[12];
        private List<Arrow> _arrows = new List<Arrow>();

        #endregion

        #region Properties

        public bool Gradient { get; set; } = true;
        public bool BorderHighlight { get; set; } = false;
        public bool IsUnicodeFont { get; set; }
        public bool DisplayLegalMoves { get; set; } = true;
        public bool IsFlipped { get; set; } = false;
        public bool DisplayGridBorders { get; set; } = false;
        public double PieceSizeFactor { get; set; } = 1;
        public string ChessFontChars { get; set; }
        public List<Point> HighlighedSquares { get; set; } = new List<Point>();

        public bool DisplayCoordinates
        {
            get
            {
                return _displayCoordinates;
            }
            set
            {
                InvalidateRender();
                _displayCoordinates = value;
            }
        }

        public string PieceFontFamily
        {
            get
            {
                return _pieceFontFamily;
            }
            set
            {
                _pieceFontFamily = value;
                _fontSizeFactor = null;

                if (_pieceFontFamily != "")
                {
                    _scaledPieceImages = GetPiecesFromFontFamily(value, _fieldSize);
                }
                else
                {
                    _renderImages = true;
                }

                Invalidate();
            }
        }

        public Color DarkSquare
        {
            get
            {
                return _darkSquare;
            }

            set
            {
                _darkSquare = value;

                int[] borderColor = new int[3];

                borderColor[0] = _darkSquare.R - 30;
                borderColor[1] = _darkSquare.G - 30;
                borderColor[2] = _darkSquare.B - 30;

                for (int i = 0; i < borderColor.Length; i++)
                {
                    if (borderColor[i] < 0)
                    {
                        borderColor[i] = 0;
                    }
                }

                _borderColor = Color.FromArgb(borderColor[0], borderColor[1], borderColor[2]);
            }
        }

        public Color LightSquare
        {
            get
            {
                return _lightSquare;
            }

            set
            {
                _lightSquare = value;
            }
        }

        public ChessGame Game
        {
            get
            {
                return _game;
            }
            set
            {
                _movingPieceIndex = new Point(-1, -1);
                _selectedIndex = new Point(-1, -1);

                _game = value;
            }
        }

        public ChessPiece[][] Board
        {
            get
            {
                return _board;
            }
        }

        public List<Arrow> Arrows
        {
            get
            {
                return _arrows;
            }

            set
            {
                _arrows = value;
            }
        }

        #endregion

        #region Events

        public event PieceMovedEventHandler PieceMoved;

        #endregion

        #region Constructors

        public ChessPanel()
        {
            DoubleBuffered = true;
        }

        #endregion

        #region Event Methods

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Game == null)
            {
                return;
            }

            Graphics g = e.Graphics;
            _board = Game.GetBoard();

            if (Width > Height)
            {
                _boardLocation = new Point(Round((Width - Height) / 2.0), 0);
                _boardDimension = Height - 1;
            }
            else
            {
                _boardLocation = new Point(0, Round((Height - Width) / 2.0));
                _boardDimension = Width - 1;
            }

            if (DisplayCoordinates)
            {
                SolidBrush borderBrush = new SolidBrush(_borderColor);
                int borderThickness = Round(_boardDimension * 0.025);

                g.FillRectangle(borderBrush, _boardLocation.X, _boardLocation.Y, _boardDimension + 1, _boardDimension + 1);
                g.DrawRectangle(Pens.Black, _boardLocation.X - 1, _boardLocation.Y - 1, _boardDimension + 2, _boardDimension + 2);

                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Font coordinateFont = new Font("Arial", (int)(borderThickness * 0.5), FontStyle.Bold);

                for (int i = 0; i < _board.Length; i++)
                {
                    string rank;
                    string file;

                    if (IsFlipped)
                    {
                        rank = (i + 1).ToString();
                        file = ((char)(Invert(i, 7) + 65)).ToString();
                    }
                    else
                    {
                        rank = (Invert(i, 7) + 1).ToString();
                        file = ((char)(i + 65)).ToString();
                    }

                    g.DrawString(rank, coordinateFont, Brushes.White, _boardLocation.X + Round(borderThickness * 0.18), _boardLocation.Y + Round(borderThickness + _fieldSize / 2.2 + _fieldSize * i));

                    g.DrawString(file, coordinateFont, Brushes.White, _boardLocation.X + Round(borderThickness + _fieldSize / 2.37 + _fieldSize * i), _boardLocation.Y + _boardDimension - Round(borderThickness * 0.85));
                }

                _boardLocation.X += borderThickness;
                _boardLocation.Y += borderThickness;

                _boardDimension -= 2 * borderThickness;

                g.DrawRectangle(Pens.Black, _boardLocation.X - 1, _boardLocation.Y - 1, _boardDimension + 2, _boardDimension + 2);
            }

            _fieldSize = _boardDimension / (double)_board.Length;

            if (_renderImages)
            {
                _scaledPieceImages = new Bitmap[12];

                bool defaultFont = false;

                if (PieceFontFamily == "" || PieceFontFamily == "Default1")
                {
                    PieceImageProvider.Inititalize(Resources.ChessPiecesSprite1, 0);
                    defaultFont = true;
                }
                else if (PieceFontFamily == "Default2")
                {
                    PieceImageProvider.Inititalize(Resources.ChessPiecesSprite2, 1);
                    defaultFont = true;
                }
                else if (PieceFontFamily == "Default3")
                {
                    PieceImageProvider.Inititalize(Resources.ChessPiecesSprite3, 2);
                    defaultFont = true;
                }

                if (defaultFont)
                {
                    for (int i = 0; i < _scaledPieceImages.Length; i++)
                    {
                        if (PieceSizeFactor > 1.5)
                        {
                            PieceSizeFactor = 1.5;
                        }

                        _scaledPieceImages[i] = ResizeImage(PieceImageProvider.PieceImages[i], (int)(_fieldSize * PieceSizeFactor), (int)(_fieldSize * PieceSizeFactor));
                    }
                }
                else
                {
                    _scaledPieceImages = GetPiecesFromFontFamily(_pieceFontFamily, _fieldSize);

                    if (_scaledPieceImages == null)
                    {
                        return;
                    }
                }

                if (Gradient)
                {
                    for (int i = 0; i < _scaledPieceImages.Length; i++)
                    {
                        _scaledPieceImages[i] = GradientBitmap(_scaledPieceImages[i]);
                    }
                }

                _renderImages = false;
            }

            SolidBrush figureBrush = null;

            float absX;
            float absY;

            int[] xLinePositions = new int[_board.Length + 1];
            int[] yLinePositions = new int[_board.Length + 1];

            int iPosition;
            for (int i = 0; i < _board.Length + 1; i++)
            {
                iPosition = Round(_fieldSize * i);
                xLinePositions[i] = iPosition + _boardLocation.X;
                yLinePositions[i] = iPosition + _boardLocation.Y;
            }

            Color evenSquare;
            Color oddSquare;

            evenSquare = LightSquare;
            oddSquare = DarkSquare;

            g.SmoothingMode = SmoothingMode.Default;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            for (int y = 0; y < Game.BoardHeight; y++)
            {
                for (int x = 0; x < Game.BoardHeight; x++)
                {
                    int index = y * Game.BoardHeight + x + y;

                    if (index % 2 == 0)
                    {
                        g.FillRectangle(new SolidBrush(evenSquare), xLinePositions[x], yLinePositions[y], xLinePositions[x + 1] - xLinePositions[x] + 1, xLinePositions[y + 1] - xLinePositions[y] + 1);
                    }
                    else
                    {
                        g.FillRectangle(new SolidBrush(oddSquare), xLinePositions[x], yLinePositions[y], xLinePositions[x + 1] - xLinePositions[x] + 1, xLinePositions[y + 1] - xLinePositions[y] + 1);
                    }
                }
            }

            for (int y = 0; y < Game.BoardHeight; y++)
            {
                for (int x = 0; x < Game.BoardHeight; x++)
                {
                    int index = y * Game.BoardHeight + x + y;
                    bool isLightSquare = index % 2 == 0;

                    for (int i = 0; i < HighlighedSquares.Count; i++)
                    {
                        if (HighlighedSquares[i].X == x && HighlighedSquares[i].Y == y && !IsFlipped ||
                            (HighlighedSquares[i].X == Invert(Game.BoardHeight - 1, x) && (HighlighedSquares[i].Y == Invert(Game.BoardHeight - 1, y) && IsFlipped)))
                        {
                            int widthCorrection = 0;
                            int heightCorrection = 0;

                            if (HighlighedSquares[i].X == Game.BoardWidth - 1 && !IsFlipped || HighlighedSquares[i].X == 0 && IsFlipped)
                            {
                                widthCorrection = 1;
                            }

                            if (HighlighedSquares[i].Y == Game.BoardWidth - 1 && !IsFlipped || HighlighedSquares[i].Y == 0 && IsFlipped)
                            {
                                heightCorrection = 1;
                            }

                            if (BorderHighlight)
                            {
                                int borderThickness = Round(_fieldSize / 18.0);
                                int offSet = borderThickness / 2;
                                borderThickness = offSet * 2;

                                Pen borderPen = new Pen(Color.Red, borderThickness);

                                g.DrawRectangle(borderPen, xLinePositions[x] + offSet, yLinePositions[y] + offSet, xLinePositions[x + 1] - xLinePositions[x] + widthCorrection - borderThickness, xLinePositions[y + 1] - xLinePositions[y] + heightCorrection - borderThickness);
                            }
                            else
                            {
                                SolidBrush highLight = new SolidBrush(Color.FromArgb(85, 255, 255, 0));

                                g.FillRectangle(highLight, xLinePositions[x], yLinePositions[y], xLinePositions[x + 1] - xLinePositions[x] + widthCorrection, xLinePositions[y + 1] - xLinePositions[y] + heightCorrection);
                            }
                        }
                    }
                }
            }

            if (Game.IsInCheck(Game.WhoseTurn))
            {
                char king = Game.WhoseTurn == ChessPlayer.White ? 'K' : 'k';
                bool kingFound = false;

                for (int y = 0; y < _board.Length; y++)
                {
                    for (int x = 0; x < _board.Length; x++)
                    {
                        if (_board[y][x] == null)
                        {
                            continue;
                        }

                        if (_board[y][x].GetFENLetter() == king)
                        {

                            int widthCorrection = 0;
                            int heightCorrection = 0;

                            if (x == Game.BoardWidth - 1 && !IsFlipped || x == 0 && IsFlipped)
                            {
                                widthCorrection = 1;
                            }

                            if (y == Game.BoardWidth - 1 && !IsFlipped || y == 0 && IsFlipped)
                            {
                                heightCorrection = 1;
                            }

                            if (IsFlipped)
                            {
                                x = Invert(Game.BoardHeight - 1, x);
                                y = Invert(Game.BoardHeight - 1, y);
                            }

                            float width = xLinePositions[x + 1] - xLinePositions[x] + widthCorrection;
                            float height = xLinePositions[y + 1] - xLinePositions[y] + heightCorrection;

                            GraphicsPath path = new GraphicsPath();
                            path.AddEllipse(xLinePositions[x], yLinePositions[y], width, height);

                            Color strongRed = Color.FromArgb(255, 255, 40, 50);
                            Color lightRed = Color.FromArgb(0, 255, 80, 80);

                            PathGradientBrush checkedWarn = new PathGradientBrush(path);
                            checkedWarn.CenterColor = strongRed;
                            checkedWarn.FocusScales = new PointF(0.5F, 0.5F);
                            checkedWarn.SurroundColors = new Color[] { lightRed };

                            g.FillEllipse(checkedWarn, xLinePositions[x], yLinePositions[y], width, height);
                            kingFound = true;
                            break;
                        }
                    }

                    if (kingFound)
                    {
                        break;
                    }
                }
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (DisplayGridBorders)
            {
                for (int i = 0; i < _board.Length + 1; i++)
                {
                    g.DrawLine(Pens.Black, _boardLocation.X, yLinePositions[i], _boardDimension + _boardLocation.X, yLinePositions[i]);
                    g.DrawLine(Pens.Black, xLinePositions[i], _boardLocation.Y, xLinePositions[i], _boardDimension + _boardLocation.Y);
                }
            }

            int maxHeight = 0;
            int heightOffset = 0;

            for (int i = 0; i < _scaledPieceImages.Length; i++)
            {
                if (_scaledPieceImages[i].Height > maxHeight)
                {
                    maxHeight = _scaledPieceImages[i].Height;
                }
            }

            heightOffset = (int)Math.Ceiling((_fieldSize - maxHeight) / 2.0);

            for (int y = 0; y < _board.Length; y++)
            {
                for (int x = 0; x < _board.Length; x++)
                {
                    if (_board[y][x] == null || (y == _movingPieceIndex.Y && x == _movingPieceIndex.X))
                    {
                        continue;
                    }

                    if (_board[y][x].Owner == ChessPlayer.White)
                    {
                        figureBrush = new SolidBrush(Color.White);
                    }
                    else
                    {
                        figureBrush = new SolidBrush(Color.Black);
                    }

                    if (!IsFlipped)
                    {
                        absX = (float)(x * _fieldSize) + _boardLocation.X;
                        absY = (float)(y * _fieldSize) + _boardLocation.Y;
                    }
                    else
                    {
                        absX = (float)(Invert(Game.BoardHeight - 1, x) * _fieldSize) + _boardLocation.X;
                        absY = (float)(Invert(Game.BoardHeight - 1, y) * _fieldSize) + _boardLocation.Y;
                    }

                    int pieceWidth = _scaledPieceImages[GetPieceIndexFromFenChar(_board[y][x].GetFENLetter())].Width;

                    absX += (float)((_fieldSize - pieceWidth) / 2.0);
                    absY += heightOffset + maxHeight - _scaledPieceImages[GetPieceIndexFromFenChar(_board[y][x].GetFENLetter())].Height;

                    g.DrawImage(_scaledPieceImages[GetPieceIndexFromFenChar(_board[y][x].GetFENLetter())], absX, absY);
                }
            }

            if (DisplayLegalMoves)
            {
                Point _possibleMovePieceIndex = new Point(-1, -1);

                if (_movingPieceIndex.X != -1)
                {
                    _possibleMovePieceIndex = _movingPieceIndex;
                }
                else if (_selectedIndex.X != -1)
                {
                    _possibleMovePieceIndex = _selectedIndex;
                }

                if (_possibleMovePieceIndex.X != -1 && _board[_possibleMovePieceIndex.Y][_possibleMovePieceIndex.X] != null)
                {
                    if (_board[_possibleMovePieceIndex.Y][_possibleMovePieceIndex.X].Owner == ChessPlayer.White)
                    {
                        figureBrush = new SolidBrush(Color.White);
                    }
                    else
                    {
                        figureBrush = new SolidBrush(Color.Black);
                    }

                    BoardPosition startPosition = new BoardPosition((ChessFile)_possibleMovePieceIndex.X, Invert(Game.BoardHeight, _possibleMovePieceIndex.Y));

                    ReadOnlyCollection<Move> validMoves = Game.GetValidMoves(startPosition);

                    for (int i = 0; i < validMoves.Count; i++)
                    {
                        Move current = validMoves[i];

                        if (!IsFlipped)
                        {
                            g.FillEllipse(_legalMoveCircleBrush, Round(((int)current.NewPosition.File) * _fieldSize + _fieldSize * 0.4) + _boardLocation.X, Round(Invert(Game.BoardHeight, current.NewPosition.Rank) * _fieldSize + _fieldSize * 0.4) + _boardLocation.Y, Round(_fieldSize * 0.2), Round(_fieldSize * 0.2));

                        }
                        else
                        {
                            g.FillEllipse(_legalMoveCircleBrush, Round((Invert(Game.BoardHeight - 1, (int)current.NewPosition.File)) * _fieldSize + _fieldSize * 0.4) + _boardLocation.X, Round((current.NewPosition.Rank - 1) * _fieldSize + _fieldSize * 0.4) + _boardLocation.Y, Round(_fieldSize * 0.2), Round(_fieldSize * 0.2));
                        }
                    }
                }
            }

            if (_arrows.Count > 0)
            {
                ArrowDrawInfo[] drawInfo = new ArrowDrawInfo[_arrows.Count];

                Point[][] arrowPositions = new Point[_arrows.Count][];
                float[] arrowDistances = new float[_arrows.Count];

                for (int i = 0; i < _arrows.Count; i++)
                {
                    drawInfo[i] = new ArrowDrawInfo();
                    drawInfo[i].Arrow = _arrows[i];
                    drawInfo[i].Positions = GetAbsPositionsFromMoveString(_arrows[i].Move);
                }

                drawInfo = drawInfo.OrderBy(c => c.Length).ToArray();

                for (int i = drawInfo.Length - 1; i >= 0; i--)
                {
                    float arrowThickness = (float)((drawInfo[i].Arrow.ThicknessPercent / 100.0 * _boardDimension));

                    if (drawInfo[i].Length / _fieldSize > 1.45)
                    {
                        arrowThickness -= 0.85F;
                    }

                    Pen arrowPen = new Pen(Color.Black, arrowThickness);
                    arrowPen.Brush = new SolidBrush(drawInfo[i].Arrow.Color);
                    arrowPen.EndCap = LineCap.ArrowAnchor;
                    arrowPen.StartCap = LineCap.RoundAnchor;

                    g.DrawLine(arrowPen, drawInfo[i].Positions[0], drawInfo[i].Positions[1]);
                }
            }

            if (_movingPieceIndex.X != -1 && _movingPoint.X != -1)
            {
                absX = _movingPoint.X - (float)(_fieldSize / 2.0);
                absY = _movingPoint.Y - (float)(_fieldSize / 2.0);

                absX += (float)((_fieldSize - _scaledPieceImages[GetPieceIndexFromFenChar(_board[_movingPieceIndex.Y][_movingPieceIndex.X].GetFENLetter())].Width) / 2);
                absY += (float)((_fieldSize - _scaledPieceImages[GetPieceIndexFromFenChar(_board[_movingPieceIndex.Y][_movingPieceIndex.X].GetFENLetter())].Height) / 2);

                g.DrawImage(_scaledPieceImages[GetPieceIndexFromFenChar(_board[_movingPieceIndex.Y][_movingPieceIndex.X].GetFENLetter())], absX, absY);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int absoluteBoardX = e.X - _boardLocation.X;
            int absoluteBoardY = e.Y - _boardLocation.Y;

            int relativeBoardX = absoluteBoardX / (int)_fieldSize;
            int relativeBoardY = absoluteBoardY / (int)_fieldSize;

            Point pieceIndex;

            if (!IsFlipped)
            {
                pieceIndex = new Point(relativeBoardX, relativeBoardY);
            }
            else
            {
                pieceIndex = new Point(Invert(Game.BoardHeight - 1, relativeBoardX), Invert(Game.BoardHeight - 1, relativeBoardY));
            }

            if (relativeBoardX < 0 || relativeBoardX > 7 || relativeBoardY < 0 || relativeBoardY > 7 || _board[pieceIndex.Y][pieceIndex.X] == null)
            {
                return;
            }

            _movingPoint = new Point(e.X, e.Y);
            _movingPieceIndex = pieceIndex;

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            int absoluteBoardX = e.X - _boardLocation.X;
            int absoluteBoardY = e.Y - _boardLocation.Y;

            int relativeBoardX;
            int relativeBoardY;

            if (!IsFlipped)
            {
                relativeBoardX = absoluteBoardX / (int)_fieldSize;
                relativeBoardY = absoluteBoardY / (int)_fieldSize;
            }
            else
            {
                relativeBoardX = Invert(Game.BoardHeight - 1, absoluteBoardX / (int)_fieldSize);
                relativeBoardY = Invert(Game.BoardHeight - 1, absoluteBoardY / (int)_fieldSize);
            }

            if (relativeBoardX < 0 || relativeBoardX > 7 || relativeBoardY < 0 || relativeBoardY > 7)
            {
                _movingPieceIndex = new Point(-1, -1);
                _selectedIndex = new Point(-1, -1);
                Invalidate();
                return;
            }

            Point currentIndex = new Point(relativeBoardX, relativeBoardY);

            if (_movingPieceIndex.X != -1 && !_movingPieceIndex.Equals(currentIndex))
            {
                PieceMoved?.Invoke(this, new PieceMovedEventArgs(_movingPieceIndex, currentIndex));
                _movingPieceIndex = new Point(-1, -1);
                _selectedIndex = new Point(-1, -1);
            }
            else
            {
                _movingPieceIndex = new Point(-1, -1);

                if (_selectedIndex.X == -1)
                {
                    if (_board[currentIndex.Y][currentIndex.X] != null && _board[currentIndex.Y][currentIndex.X].Owner == Game.WhoseTurn)
                    {
                        _selectedIndex = currentIndex;
                    }
                }
                else if (!_selectedIndex.Equals(currentIndex))
                {
                    if (_board[currentIndex.Y][currentIndex.X] != null &&
                        _board[_selectedIndex.Y][_selectedIndex.X] != null &&
                        _board[currentIndex.Y][currentIndex.X].Owner == _board[_selectedIndex.Y][_selectedIndex.X].Owner)
                    {
                        _selectedIndex = currentIndex;
                    }
                    else
                    {
                        PieceMoved?.Invoke(this, new PieceMovedEventArgs(_selectedIndex, currentIndex));

                        _movingPieceIndex = new Point(-1, -1);
                        _selectedIndex = new Point(-1, -1);
                    }
                }
            }

            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_movingPieceIndex.X != -1)
            {
                _movingPoint = new Point(e.X, e.Y);
                Invalidate();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            _renderImages = true;
            Invalidate();
        }

        #endregion

        #region Other Methods

        public void InvalidateRender()
        {
            _renderImages = true;
            Invalidate();
        }

        private void FloodFill(Bitmap image, int x, int y)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            LinkedList<Point> check = new LinkedList<Point>();

            int recordLength = 4;
            int[] bits = new int[data.Stride / recordLength * data.Height];

            Marshal.Copy(data.Scan0, bits, 0, bits.Length);

            List<int> smoothPixelLocations = new List<int>();
            List<int> smoothPixelColors = new List<int>();

            int border = -16777216;
            int alpha1 = 16777216;
            int floodTo = 16777215;
            int floodFrom = bits[x + y * data.Stride / recordLength];

            bits[x + y * data.Stride / recordLength] = floodTo;

            Point[] offSets = new Point[]
            {
                new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0)
            };

            if (floodFrom != floodTo)
            {
                check.AddLast(new Point(x, y));

                while (check.Count > 0)
                {
                    Point current = check.First.Value;
                    check.RemoveFirst();

                    foreach (Point offSet in offSets)
                    {
                        Point next = new Point(current.X + offSet.X, current.Y + offSet.Y);

                        if (next.X >= 0 && next.Y >= 0 && next.X < data.Width && next.Y < data.Height)
                        {
                            if (bits[next.X + next.Y * data.Stride / recordLength] != border && (bits[next.X + next.Y * data.Stride / recordLength] > alpha1 || bits[next.X + next.Y * data.Stride / recordLength] < 0))
                            {
                                check.AddLast(next);

                                if (bits[next.X + next.Y * data.Stride / recordLength] != floodFrom)
                                {
                                    smoothPixelLocations.Add(next.X + next.Y * data.Stride / recordLength);
                                    smoothPixelColors.Add(bits[next.X + next.Y * data.Stride / recordLength]);
                                }

                                bits[next.X + next.Y * data.Stride / recordLength] = floodTo;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < smoothPixelLocations.Count; i++)
            {
                int colorInteger = smoothPixelColors[i];
                Color color = Color.FromArgb(colorInteger);

                bits[smoothPixelLocations[i]] = (Color.FromArgb(Math.Abs(color.R - 255), 0, 0, 0)).ToArgb();
            }

            Marshal.Copy(bits, 0, data.Scan0, bits.Length);
            image.UnlockBits(data);
        }

        private int Round(double number)
        {
            return (int)Math.Round(number, 0);
        }

        private int Invert(int max, int value)
        {
            return Math.Abs(value - max);
        }

        private int GetPieceIndexFromFenChar(char fenCharacter)
        {
            switch (fenCharacter)
            {
                case 'K':
                    return 0;
                case 'k':
                    return 6;
                case 'Q':
                    return 1;
                case 'q':
                    return 7;
                case 'R':
                    return 4;
                case 'r':
                    return 10;
                case 'B':
                    return 2;
                case 'b':
                    return 8;
                case 'N':
                    return 3;
                case 'n':
                    return 9;
                case 'P':
                    return 5;
                case 'p':
                    return 11;
            }

            return -1;
        }

        private Bitmap FillTransparentSectors(Bitmap image)
        {
            Bitmap filledImage;
            filledImage = TransparentToColor(image, Color.White);
            FloodFill(filledImage, 0, 0);
            return filledImage;
        }

        private Bitmap TransparentToColor(Bitmap image, Color color)
        {
            Bitmap filledImage = new Bitmap(image.Width, image.Height);
            Rectangle rectangle = new Rectangle(Point.Empty, image.Size);

            using (Graphics g = Graphics.FromImage(filledImage))
            {
                g.Clear(color);
                g.DrawImageUnscaledAndClipped(image, rectangle);
            }

            return filledImage;
        }

        private Bitmap GradientBitmap(Bitmap image)
        {
            unsafe
            {
                BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte* pointer = (byte*)data.Scan0;

                int startingPoint = (int)(image.Width * 0.4);

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = startingPoint; x < image.Width; x++)
                    {
                        int diffValue = (int)(255 * ((x - startingPoint) / (double)image.Width * 0.225));

                        pointer = (byte*)(data.Scan0 + y * data.Stride + x * 4);

                        if (pointer[3] == 255)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int value = pointer[i] - diffValue;

                                if (value < 0)
                                {
                                    value = 0;
                                }

                                pointer[i] = (byte)value;
                            }
                        }
                    }
                }

                image.UnlockBits(data);
            }

            return image;
        }

        private Bitmap CropTransparentBorders(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            Func<int, bool> allTransparentRow = (row) =>
            {
                for (int i = 0; i < width; ++i)
                    if (image.GetPixel(i, row).A != 0)
                        return false;
                return true;
            };

            Func<int, bool> allTransparentColumn = (column) =>
            {
                for (int i = 0; i < height; ++i)
                    if (image.GetPixel(column, i).A != 0)
                        return false;
                return true;
            };

            int topmost = 0;
            for (int row = 0; row < height; ++row)
            {
                if (allTransparentRow(row))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = height - 1; row >= 0; --row)
            {
                if (allTransparentRow(row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0;
            int rightmost = 0;

            for (int column = 0; column < width; ++column)
            {
                if (allTransparentColumn(column))
                    leftmost = column;
                else
                    break;
            }

            for (int column = width - 1; column >= 0; --column)
            {
                if (allTransparentColumn(column))
                    rightmost = column;
                else
                    break;
            }

            int croppedWidth = rightmost + 1 - leftmost;
            int croppedHeight = bottommost + 1 - topmost;

            try
            {
                Bitmap target = new Bitmap(croppedWidth, croppedHeight);

                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(image, new RectangleF(0, 0, croppedWidth, croppedHeight), new RectangleF(leftmost, topmost, croppedWidth, croppedHeight), GraphicsUnit.Pixel);
                }

                return target;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return image;
            }
        }

        private Bitmap GetCharacterImage(string fontFamily, int fontSize, char character)
        {
            Font font = new Font(fontFamily, fontSize);
            SizeF drawSize = TextRenderer.MeasureText(character.ToString(), font);
            Bitmap charImage = new Bitmap((int)drawSize.Width, (int)drawSize.Height);
            Graphics g = Graphics.FromImage(charImage);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.DrawString(character.ToString(), font, Brushes.Black, 0, 0);
            return charImage;
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(image, 0, 0, result.Width, result.Height);
            }

            return result;
        }

        public Point[] GetRelPositionsFromMoveString(string move)
        {
            Point[] index = new Point[2];
            index[0] = new Point(move[0] - 97, Invert(Game.BoardHeight, int.Parse(move[1].ToString())));
            index[1] = new Point(move[2] - 97, Invert(Game.BoardHeight, int.Parse(move[3].ToString())));

            return index;
        }

        public PointF[] GetAbsPositionsFromMoveString(string move)
        {
            PointF[] index = new PointF[2];

            double offSet = 0.5;

            if (!IsFlipped)
            {
                index[0] = new PointF((float)(((move[0] - 97) + offSet) * _fieldSize + _boardLocation.X - 0.45F), (float)(((Invert(Game.BoardHeight, int.Parse(move[1].ToString())) + offSet) * _fieldSize) + _boardLocation.Y));
                index[1] = new PointF((float)((((move[2] - 97) + offSet) * _fieldSize) + _boardLocation.X - 0.45F), (float)(((Invert(Game.BoardHeight, int.Parse(move[3].ToString())) + offSet) * _fieldSize) + _boardLocation.Y));
            }
            else
            {
                index[0] = new PointF((float)(((Invert(Game.BoardHeight - 1, move[0] - 97) + offSet) * _fieldSize) + _boardLocation.X - 0.70F), (float)(((int.Parse(move[1].ToString()) - 1 + offSet) * _fieldSize) + _boardLocation.Y));
                index[1] = new PointF((float)(((Invert(Game.BoardHeight - 1, move[2] - 97) + offSet) * _fieldSize) + _boardLocation.X - 0.70F), (float)(((int.Parse(move[3].ToString()) - 1 + offSet) * _fieldSize) + _boardLocation.Y));
            }

            return index;
        }

        private Bitmap[] GetPiecesFromFontFamily(string fontFamily, double fieldSize)
        {
            if (fieldSize == 0)
            {
                return null;
            }

            Bitmap[] pieceImages = new Bitmap[12];

            int minFontSize = 125;
            int whiteKing = 0x2654;

            char[] characters;

            if (IsUnicodeFont || ChessFontChars == null || ChessFontChars.Length != 12)
            {
                characters = new char[]
                {
                    '♔', '♕', '♗', '♘', '♖', '♙',
                    '♚', '♛', '♝', '♞', '♜', '♟'
                };
            }
            else
            {
                characters = ChessFontChars.ToCharArray();
            }

            if (_fontSizeFactor == null)
            {
                _fontSizeFactor = double.MaxValue;

                for (int i = 0; i < 6; i++)
                {
                    Bitmap image = GetCharacterImage(fontFamily, (int)fieldSize, characters[i]);
                    Bitmap croppedImage = CropTransparentBorders(image);

                    double widthOffset = (double)image.Width / croppedImage.Width - 1;
                    double heightOffset = (double)image.Height / croppedImage.Height - 1;

                    if (widthOffset < _fontSizeFactor)
                    {
                        _fontSizeFactor = widthOffset;
                    }

                    if (heightOffset < _fontSizeFactor)
                    {
                        _fontSizeFactor = heightOffset;
                    }
                }
            }

            int fontSize = -1;
            SizeF currentDimension = new SizeF(-1, -1);

            int fontSizeCounter = 0;

            while (currentDimension.Height < fieldSize && currentDimension.Width < fieldSize)
            {
                fontSizeCounter++;

                Font font = new Font(fontFamily, fontSizeCounter);
                currentDimension = TextRenderer.MeasureText(((char)whiteKing).ToString(), font);
            }

            fontSize = (int)((fontSizeCounter * (1 + _fontSizeFactor)) * PieceSizeFactor);

            if (fontSize > (int)_fieldSize + 1)
            {
                fontSize = (int)_fieldSize;
            }

            for (int i = 0; i < pieceImages.Length; i++)
            {
                Bitmap originalImage = null;

                if (fontSize < minFontSize)
                {
                    originalImage = CropTransparentBorders(GetCharacterImage(fontFamily, fontSize, characters[i]));
                    pieceImages[i] = CropTransparentBorders(GetCharacterImage(fontFamily, minFontSize, characters[i]));
                }
                else
                {
                    pieceImages[i] = CropTransparentBorders(GetCharacterImage(fontFamily, fontSize, characters[i]));
                }

                pieceImages[i] = FillTransparentSectors(pieceImages[i]);

                if (fontSize < minFontSize)
                {
                    pieceImages[i] = ResizeImage(pieceImages[i], originalImage.Width, originalImage.Height);
                }
            }

            return pieceImages;
        }

        #endregion
    }
}