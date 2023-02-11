using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocona.ShellCompletion.Candidate;
using Tiled2Dmap.CLI.ImageHelp;
using Microsoft.Xna.Framework.Content;

namespace Tiled2Dmap.CLI.ImageServices
{
    internal class ImageFontIS
    {
        private const int maxNumberWidth = 5;
        private const int spaceCount = 1;//Number of pixels between each character.

        private readonly Image<Rgba32> _fontImage;
        private readonly Image<Rgba32> _negativeImage;
        private readonly Size _characterSize;
        private readonly List<int> _values;

        public Size NumberSize { get; init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fontImage">Image containing font characters with a fixed width.</param>
        internal ImageFontIS(Image<Rgba32> fontImage, Image<Rgba32> negativeImage, List<int> values, int numCharacters = 10) 
        { 
            _fontImage = fontImage;
            _negativeImage = negativeImage;
            _values = values;
            _characterSize = new(_fontImage.Width / numCharacters, _fontImage.Height);

            //Calculate the Number Image size;
            int maxLength = _values.Max(x => x.ToString().Length);
            int widthPx = (_characterSize.Width + spaceCount) * maxLength - spaceCount;

            NumberSize = new(widthPx, _characterSize.Height);
        }


        internal Image<Rgba32> GetNumbersImage()
        {
            int numValues = _values.Count();

            int rows = 1;
            int cols = numValues;

            if(numValues > maxNumberWidth)
            {
                rows = numValues / maxNumberWidth + 1;
                cols = maxNumberWidth;
            }

            Image<Rgba32> image = new Image<Rgba32>(NumberSize.Width * cols, rows * _characterSize.Height);

            //Copy the individual number images to the overall image.

            //Keep track of the current numbers bounding rectangle.
            var numberBounds = new Rectangle(0,0, NumberSize.Width, _characterSize.Height);

            for(int i = 0; i < numValues; i++)
            {
                //Adjust the numberBounds rectangle
                numberBounds.X = i % maxNumberWidth * NumberSize.Width;
                numberBounds.Y = i / maxNumberWidth * _characterSize.Height;

                string numbersText = _values[i].ToString();

                //Determine tthe number of unused pixels to keep the text centered..
                int numTotalLength = (_characterSize.Width + spaceCount) * numbersText.Length - spaceCount;
                int totalPadding = NumberSize.Width - numTotalLength;
                int leftPadding = totalPadding / 2;

                //The characters location in image
                Point characterLocation = new(numberBounds.X + leftPadding, numberBounds.Y);

                for(int j = 0; j < numbersText.Length; j++)
                {
                    if (numbersText[j] == '-')
                    {
                        _negativeImage.CopyTo(new Rectangle(new(0, 0), _negativeImage.Size()), image, characterLocation);
                    }
                    else
                    { 
                        int digitOffset = (numbersText[j] - 0x30) * _characterSize.Width;
                        var digitBound = new Rectangle(new Point(digitOffset, 0), _characterSize);
                        _fontImage.CopyTo(digitBound, image, characterLocation);

                    }
                    characterLocation.X += _characterSize.Width + spaceCount;
                }
            }

            return image;
        }
    }
}
