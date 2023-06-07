using System.Drawing;

namespace BlazorFeste.lib
{
  public class ThermalMemoryStream
  {
    private MemoryStream _memoryStream = new  MemoryStream();
    
    private byte _maxPrintingDots = 7;
    private byte _heatingTime = 80;
    private byte _heatingInterval = 2;

    /// <summary>
    /// Delay between two picture lines. (in ms)
    /// </summary>
    public int PictureLineSleepTimeMs = 40;
    /// <summary>
    /// Delay between two text lines. (in ms)
    /// </summary>
    public int WriteLineSleepTimeMs = 0;
    /// <summary>
    /// Current encoding used by the printer.
    /// </summary>
    //public int Encoding { get; private set; }
    public string Encoding { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThermalDotNet.ThermalPrinter"/> class.
    /// </summary>
    /// <param name='memoryStream'>
    /// Serial port used by printer.
    /// </param>
    /// <param name='maxPrintingDots'>
    /// Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 ((7+1)*8 = 64 dots)
    /// </param>
    /// <param name='heatingTime'>
    /// Heating time (3-255), unit: 10µs, default: 80 (800µs)
    /// </param>
    /// <param name='heatingInterval'>
    /// Heating interval (0-255), unit: 10µs, default: 2 (20µs)
    /// </param>
    public ThermalMemoryStream(MemoryStream memoryStream, byte maxPrintingDots, byte heatingTime, byte heatingInterval) => Constructor(memoryStream, maxPrintingDots, heatingTime, heatingInterval);

    /// <summary>
    /// Initializes a new instance of the <see cref="ThermalDotNet.ThermalPrinter"/> class.
    /// </summary>
    /// <param name='memoryStream'>
    /// Serial port used by printer.
    /// </param>
    public ThermalMemoryStream(MemoryStream memoryStream)
    {
      Constructor(memoryStream, _maxPrintingDots, _heatingTime, _heatingInterval);
    }

    private void Constructor(MemoryStream memoryStream, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
    {
      System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

      this.Encoding = "IBM437";

      _maxPrintingDots = maxPrintingDots;
      _heatingTime = heatingTime;
      _heatingInterval = heatingInterval;

      _memoryStream = memoryStream;

      Reset();
      SetPrintingParameters(maxPrintingDots, heatingTime, heatingInterval);
      SendEncoding("IBM437");

      Reset();
    }

    private void WriteByte(byte valueToWrite)
    {
      byte[] tempArray = { valueToWrite };
      _memoryStream.Write(tempArray, 0, tempArray.Length);  //  _serialPort.Write(tempArray, 0, 1);
    }

    /// <summary>
    /// Prints the line of text.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    public void WriteLine(string text)
    {
      WriteToBuffer(text);
      WriteByte(10);
      System.Threading.Thread.Sleep(WriteLineSleepTimeMs);
    }

    /// <summary>
    /// Sends the text to the printer buffer. Does not print until a line feed (0x10) is sent.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    public void WriteToBuffer(string text)
    {
      text = text.Trim('\n').Trim('\r');
      byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes(text);
      byte[] outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.GetEncoding(this.Encoding), originalBytes);
      _memoryStream.Write(outputBytes, 0, outputBytes.Length);  // _serialPort.Write(outputBytes, 0, outputBytes.Length);
    }

    /// <summary>
    /// Prints the line of text, white on black.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    public void WriteLine_Invert(string text)
    {
      //Sets inversion on
      WriteByte(29);
      WriteByte(66);
      WriteByte(1);

      //Sends the text
      WriteLine(text);

      //Sets inversion off
      WriteByte(29);
      WriteByte(66);
      WriteByte(0);

      LineFeed();
    }

    /// <summary>
    /// Prints the line of text, double size.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    public void WriteLine_Big(string text)
    {
      const byte DoubleHeight = 1 << 4;
      const byte DoubleWidth = 1 << 5;
      const byte Bold = 1 << 3;

      //big on
      WriteByte(27);
      WriteByte(33);
      WriteByte(DoubleHeight + DoubleWidth + Bold);

      //Sends the text
      WriteLine(text);

      //big off
      WriteByte(27);
      WriteByte(33);
      WriteByte(0);
    }

    /// <summary>
    /// Prints the line of text.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    /// <param name='style'>
    /// Style of the text.
    /// </param> 
    public void WriteLine(string text, PrintingStyle style)
    {
      WriteLine(text, (byte)style);
    }

    public void SelectFont(int iFont)
    {
      WriteByte(27);
      WriteByte(33);
      WriteByte((byte)iFont);
    }

    /// <summary>
    /// Prints the line of text.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    /// <param name='style'>
    /// Style of the text. Can be the sum of PrintingStyle enums.
    /// </param>
    public void WriteLine(string text, byte style)
    {
      byte underlineHeight = 0;

      if (BitTest(style, 0))
      {
        style = BitClear(style, 0);
        underlineHeight = 1;
      }

      if (BitTest(style, 7))
      {
        style = BitClear(style, 7);
        underlineHeight = 2;
      }

      if (underlineHeight != 0)
      {
        WriteByte(27);
        WriteByte(45);
        WriteByte(underlineHeight);
      }

      //style on
      WriteByte(27);
      WriteByte(33);
      WriteByte((byte)style);

      //Sends the text
      WriteLine(text);

      //style off
      if (underlineHeight != 0)
      {
        WriteByte(27);
        WriteByte(45);
        WriteByte(0);
      }
      WriteByte(27);
      WriteByte(33);
      WriteByte(0);

    }

    /// <summary>
    /// Prints the line of text in bold.
    /// </summary>
    /// <param name='text'>
    /// Text to print.
    /// </param>
    public void WriteLine_Bold(string text)
    {
      //bold on
      BoldOn();

      //Sends the text
      WriteLine(text);

      //bold off
      BoldOff();

      LineFeed();
    }

    /// <summary>
    /// Sets bold mode on.
    /// </summary>
    public void BoldOn()
    {
      WriteByte(27);
      WriteByte(32);
      WriteByte(1);
      WriteByte(27);
      WriteByte(69);
      WriteByte(1);
    }

    /// <summary>
    /// Sets bold mode off.
    /// </summary>
    public void BoldOff()
    {
      WriteByte(27);
      WriteByte(32);
      WriteByte(0);
      WriteByte(27);
      WriteByte(69);
      WriteByte(0);
    }

    /// <summary>
    /// Sets white on black mode on.
    /// </summary>
    public void WhiteOnBlackOn()
    {
      WriteByte(29);
      WriteByte(66);
      WriteByte(1);
    }

    /// <summary>
    /// Sets white on black mode off.
    /// </summary>
    public void WhiteOnBlackOff()
    {
      WriteByte(29);
      WriteByte(66);
      WriteByte(0);
    }

    /// <summary>
    /// Sets the text size.
    /// </summary>
    /// <param name='doubleWidth'>
    /// Double width
    /// </param>
    /// <param name='doubleHeight'>
    /// Double height
    /// </param>
    public void SetSize(bool doubleWidth, bool doubleHeight)
    {
      int sizeValue = (Convert.ToInt32(doubleWidth)) * (0xF0) + (Convert.ToInt32(doubleHeight)) * (0x0F);
      WriteByte(29);
      WriteByte(33);
      WriteByte((byte)sizeValue);
    }

    ///	<summary>
    /// Send Cut Request to printer
    /// </summary>
    public void CutRequest()
    {
      WriteByte(29);
      WriteByte(86);
      WriteByte(0);

      //      _writeByte(27);
      //      _writeByte(105);
    }

    ///	<summary>
    /// Eject Ticket
    /// </summary>
    public void EjectTicket()
    {
      WriteByte(29);
      WriteByte(101);
      WriteByte(5);
    }

    ///	<summary>
    /// Prints the contents of the buffer and feeds one line.
    /// </summary>
    public void LineFeed()
    {
      WriteByte(10);
    }

    /// <summary>
    /// Prints the contents of the buffer and feeds n lines.
    /// </summary>
    /// <param name='lines'>
    /// Number of lines to feed.
    /// </param>
    public void LineFeed(byte lines)
    {
      WriteByte(27);
      WriteByte(100);
      WriteByte(lines);
    }

    /// <summary>
    /// Idents the text.
    /// </summary>
    /// <param name='columns'>
    /// Number of columns.
    /// </param>
    public void Indent(byte columns)
    {
      if (columns < 0 || columns > 35)
      {
        columns = 0;
      }

      WriteByte(27);
      WriteByte(66);
      WriteByte(columns);
    }

    /// <summary>
    /// Sets the line spacing.
    /// </summary>
    /// <param name='lineSpacing'>
    /// Line spacing (in dots), default value: 32 dots.
    /// </param>
    public void SetLineSpacing(byte lineSpacing)
    {
      WriteByte(27);
      WriteByte(51);
      WriteByte(lineSpacing);
    }

    /// <summary>
    /// Aligns the text to the left.
    /// </summary>
    public void SetAlignLeft()
    {
      WriteByte(27);
      WriteByte(97);
      WriteByte(0);
    }

    /// <summary>
    /// Centers the text.
    /// </summary>		
    public void SetAlignCenter()
    {
      WriteByte(27);
      WriteByte(97);
      WriteByte(1);
    }

    /// <summary>
    /// Aligns the text to the right.
    /// </summary>
    public void SetAlignRight()
    {
      WriteByte(27);
      WriteByte(97);
      WriteByte(2);
    }

    /// <summary>
    /// Prints a horizontal line.
    /// </summary>
    /// <param name='length'>
    /// Line length (in characters) (max 42).
    /// </param>
    public void HorizontalLine(int length)
    {
      if (length > 0)
      {
        if (length > 42)
        {
          length = 42;
        }

        for (int i = 0; i < length; i++)
        {
          WriteByte(0xC4);
        }
        WriteByte(10);
      }
    }

    /// <summary>
    /// Resets the printer.
    /// </summary>
    public void Reset()
    {
      WriteByte(27);
      WriteByte(64);
      System.Threading.Thread.Sleep(50);
    }

    /// <summary>
    /// List of supported barcode types.
    /// </summary>
    public enum BarcodeType
    {
      /// <summary>
      /// UPC-A
      /// </summary>
      upc_a = 0,
      /// <summary>
      /// UPC-E
      /// </summary>
      upc_e = 1,
      /// <summary>
      /// EAN13
      /// </summary>
      ean13 = 2,
      /// <summary>
      /// EAN8
      /// </summary>
      ean8 = 3,
      /// <summary>
      /// CODE 39
      /// </summary>
      code39 = 4,
      /// <summary>
      /// I25
      /// </summary>
      i25 = 5,
      /// <summary>
      /// CODEBAR
      /// </summary>
      codebar = 6,
      /// <summary>
      /// CODE 93
      /// </summary>
      code93 = 7,
      /// <summary>
      /// CODE 128
      /// </summary>
      code128 = 8,
      /// <summary>
      /// CODE 11
      /// </summary>
      code11 = 9,
      /// <summary>
      /// MSI
      /// </summary>
      msi = 10
    }

    /// <summary>
    /// Prints the barcode data.
    /// </summary>
    /// <param name='type'>
    /// Type of barcode.
    /// </param>
    /// <param name='data'>
    /// Data to print.
    /// </param>
    public void PrintBarcode(BarcodeType type, string data)
    {
      byte[] originalBytes;
      byte[] outputBytes;

      if (type == BarcodeType.code93 || type == BarcodeType.code128)
      {
        originalBytes = System.Text.Encoding.UTF8.GetBytes(data);
        outputBytes = originalBytes;
      }
      else
      {
        originalBytes = System.Text.Encoding.UTF8.GetBytes(data.ToUpper());
        outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.GetEncoding(this.Encoding), originalBytes);
      }

      switch (type)
      {
        case BarcodeType.upc_a:
          if (data.Length == 11 || data.Length == 12)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(0);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.upc_e:
          if (data.Length == 11 || data.Length == 12)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(1);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.ean13:
          if (data.Length == 12 || data.Length == 13)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(2);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.ean8:
          if (data.Length == 7 || data.Length == 8)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(3);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.code39:
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(4);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.i25:
          if (data.Length > 1 || data.Length % 2 == 0)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(5);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.codebar:
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(6);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.code93: //todo: overload PrintBarcode method with a byte array parameter
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(7); //todo: use format 2 (init string : 29,107,72) (0x00 can be a value, too)
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.code128: //todo: overload PrintBarcode method with a byte array parameter
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(8); //todo: use format 2 (init string : 29,107,73) (0x00 can be a value, too)
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.code11:
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(9);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
        case BarcodeType.msi:
          if (data.Length > 1)
          {
            WriteByte(29);
            WriteByte(107);
            WriteByte(10);
            _memoryStream.Write(outputBytes, 0, data.Length); // _serialPort.Write(outputBytes, 0, data.Length);
            WriteByte(0);
          }
          break;
      }
    }

    /// <summary>
    /// Selects large barcode mode.
    /// </summary>
    /// <param name='large'>
    /// Large barcode mode.
    /// </param>
    public void SetLargeBarcode(bool large)
    {
      if (large)
      {
        WriteByte(29);
        WriteByte(119);
        WriteByte(3);
      }
      else
      {
        WriteByte(29);
        WriteByte(119);
        WriteByte(2);
      }
    }

    /// <summary>
    /// Sets the barcode left space.
    /// </summary>
    /// <param name='spacingDots'>
    /// Spacing dots.
    /// </param>
    public void SetBarcodeLeftSpace(byte spacingDots)
    {
      WriteByte(29);
      WriteByte(120);
      WriteByte(spacingDots);
    }

    /// <summary>
    /// Prints the image. The image must be 384px wide.
    /// </summary>
    /// <param name='fileName'>
    /// Image file path.
    /// </param>
    public void PrintImage(string fileName)
    {

      if (!File.Exists(fileName))
      {
        throw (new Exception("File does not exist."));
      }

      using (Bitmap image = new Bitmap(fileName))
      {
        PrintImage(image);
      }
    }

    /// <summary>
    /// Prints the image. The image must be 384px wide.
    /// </summary>
    /// <param name='image'>
    /// Image to print.
    /// </param>
    public void PrintImage(Bitmap image)
    {
      int width = image.Width;
      int height = image.Height;

      byte[,] imgArray = new byte[width, height];

      if (width != 384 || height > 65635)
      {
        throw (new Exception("Image width must be 384px, height cannot exceed 65635px."));
      }

      //Processing image data	
      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < (image.Width / 8); x++)
        {
          imgArray[x, y] = 0;
          for (byte n = 0; n < 8; n++)
          {
            Color pixel = image.GetPixel(x * 8 + n, y);
            if (pixel.GetBrightness() < 0.5)
            {
              imgArray[x, y] += (byte)(1 << n);
            }
          }
        }
      }

      //Print LSB first bitmap
      WriteByte(18);
      WriteByte(118);

      WriteByte((byte)(height & 255));   //height LSB
      WriteByte((byte)(height >> 8));  //height MSB


      for (int y = 0; y < height; y++)
      {
        System.Threading.Thread.Sleep(PictureLineSleepTimeMs);
        for (int x = 0; x < (width / 8); x++)
        {
          WriteByte(imgArray[x, y]);
        }
      }
    }

    /// <summary>
    /// Sets the printing parameters.
    /// </summary>
    /// <param name='maxPrintingDots'>
    /// Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 (beceause (7+1)*8 = 64 dots)
    /// </param>
    /// <param name='heatingTime'>
    /// Heating time (3-255), unit: 10µs, default: 80 (800µs)
    /// </param>
    /// <param name='heatingInterval'>
    /// Heating interval (0-255), unit: 10µs, default: 2 (20µs)
    /// </param>
    public void SetPrintingParameters(byte maxPrintingDots, byte heatingTime, byte heatingInterval)
    {
      WriteByte(27);
      WriteByte(55);
      WriteByte(maxPrintingDots);
      WriteByte(heatingTime);
      WriteByte(heatingInterval);
    }

    /// <summary>
    /// Sets the printer offine.
    /// </summary>
    public void Sleep()
    {
      WriteByte(27);
      WriteByte(61);
      WriteByte(0);
    }

    /// <summary>
    /// Sets the printer online.
    /// </summary>		
    public void WakeUp()
    {
      WriteByte(27);
      WriteByte(61);
      WriteByte(1);
    }

    /// <summary>
    /// Returns a <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.
    /// </returns>
    public override string ToString()
    {
      return $"ThermalMemoryStream:\n\t_maxPrintingDots={_maxPrintingDots}," +
        $"\n\t_heatingTime={_heatingTime},\n\t_heatingInterval={_heatingInterval},\n\tPictureLineSleepTimeMs={PictureLineSleepTimeMs}," +
        $"\n\tWriteLineSleepTimeMs={WriteLineSleepTimeMs},\n\tEncoding={Encoding}";
    }

    /// <summary>
    /// Returns a printing style.
    /// </summary>
    public enum PrintingStyle
    {
      /// <summary>
      /// White on black.
      /// </summary>
      Reverse = 1 << 1,
      /// <summary>
      /// Updown characters.
      /// </summary>
      Updown = 1 << 2,
      /// <summary>
      /// Bold characters.
      /// </summary>
      Bold = 1 << 3,
      /// <summary>
      /// Double height characters.
      /// </summary>
      DoubleHeight = 1 << 4,
      /// <summary>
      /// Double width characters.
      /// </summary>
      DoubleWidth = 1 << 5,
      /// <summary>
      /// Strikes text.
      /// </summary>
      DeleteLine = 1 << 6,
      /// <summary>
      /// Thin underline.
      /// </summary>
      Underline = 1 << 0,
      /// <summary>
      /// Thick underline.
      /// </summary>
      ThickUnderline = 1 << 7
    }

    /// <summary>
    /// Prints the contents of the buffer and feeds n dots.
    /// </summary>
    /// <param name='dotsToFeed'>
    /// Number of dots to feed.
    /// </param>
    public void FeedDots(byte dotsToFeed)
    {
      WriteByte(27);
      WriteByte(74);
      WriteByte(dotsToFeed);
    }

    private void SetMargins(byte leftMargin, byte rightMargin)
    {
      WriteByte(27);
      WriteByte(1);
      WriteByte(leftMargin);

      WriteByte(27);
      WriteByte(81);
      WriteByte(rightMargin);
    }

    private void SendEncoding(string encoding)
    {
      switch (encoding)
      {
        case "IBM437":
          WriteByte(27);
          WriteByte(116);
          WriteByte(0);
          break;

        case "ibm850":
          WriteByte(27);
          WriteByte(116);
          WriteByte(1);
          break;
      }
    }

    /// <summary>
    /// Tests the value of a given bit.
    /// </summary>
    /// <param name="valueToTest">The value to test</param>
    /// <param name="testBit">The bit number to test</param>
    /// <returns></returns>
    static private bool BitTest(byte valueToTest, int testBit)
    {
      return ((valueToTest & (byte)(1 << testBit)) == (byte)(1 << testBit));
    }

#pragma warning disable IDE0051 // Rimuovi i membri privati inutilizzati
    /// <summary>
    /// Return the given value with its n bit set.
    /// </summary>
    /// <param name="originalValue">The value to return</param>
    /// <param name="bit">The bit number to set</param>
    /// <returns></returns>
    static private byte BitSet(byte originalValue, byte bit)
#pragma warning restore IDE0051 // Rimuovi i membri privati inutilizzati
    {
      return originalValue |= (byte)((byte)1 << bit);
    }

    /// <summary>
    /// Return the given value with its n bit cleared.
    /// </summary>
    /// <param name="originalValue">The value to return</param>
    /// <param name="bit">The bit number to clear</param>
    /// <returns></returns>
    static private byte BitClear(byte originalValue, int bit)
    {
      return originalValue &= (byte)(~(1 << bit));
    }
  }
}
