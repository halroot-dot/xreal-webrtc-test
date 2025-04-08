using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Json
{
    public static object Deserialize(string json)
    {
        if (json == null)
        {
            return null;
        }
        return Parser.Parse(json);
    }

    private sealed class Parser : IDisposable
    {
        private const string WordBreak = "{}[],:\"";
        private StringReader json;

        private Parser(string jsonString)
        {
            json = new StringReader(jsonString);
        }

        public static object Parse(string jsonString)
        {
            using (var instance = new Parser(jsonString))
            {
                return instance.ParseValue();
            }
        }

        public void Dispose()
        {
            json.Dispose();
            json = null;
        }

        private Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> table = new Dictionary<string, object>();

            json.Read(); // {

            while (true)
            {
                switch (NextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.CURLY_CLOSE:
                        return table;
                    default:
                        string name = ParseString();
                        if (name == null)
                        {
                            return null;
                        }

                        if (NextToken != TOKEN.COLON)
                        {
                            return null;
                        }
                        json.Read(); // :

                        table[name] = ParseValue();
                        break;
                }
            }
        }

        private List<object> ParseArray()
        {
            List<object> array = new List<object>();
            json.Read(); // [

            while (true)
            {
                switch (NextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.SQUARED_CLOSE:
                        return array;
                    default:
                        object value = ParseValue();
                        array.Add(value);
                        break;
                }
            }
        }

        private object ParseValue()
        {
            switch (NextToken)
            {
                case TOKEN.STRING:
                    return ParseString();
                case TOKEN.NUMBER:
                    return ParseNumber();
                case TOKEN.CURLY_OPEN:
                    return ParseObject();
                case TOKEN.SQUARED_OPEN:
                    return ParseArray();
                case TOKEN.TRUE:
                    return true;
                case TOKEN.FALSE:
                    return false;
                case TOKEN.NULL:
                    return null;
                default:
                    return null;
            }
        }

        private string ParseString()
        {
            StringBuilder s = new StringBuilder();
            json.Read(); // "

            bool parsing = true;
            while (parsing)
            {
                if (json.Peek() == -1)
                {
                    parsing = false;
                    break;
                }

                char c = NextChar;
                switch (c)
                {
                    case '"':
                        parsing = false;
                        break;
                    case '\\':
                        if (json.Peek() == -1)
                        {
                            parsing = false;
                            break;
                        }

                        c = NextChar;
                        switch (c)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                s.Append(c);
                                break;
                            case 'b':
                                s.Append('\b');
                                break;
                            case 'f':
                                s.Append('\f');
                                break;
                            case 'n':
                                s.Append('\n');
                                break;
                            case 'r':
                                s.Append('\r');
                                break;
                            case 't':
                                s.Append('\t');
                                break;
                            case 'u':
                                char[] hex = new char[4];
                                for (int i = 0; i < 4; i++)
                                {
                                    hex[i] = NextChar;
                                }
                                s.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                        break;
                    default:
                        s.Append(c);
                        break;
                }
            }

            return s.ToString();
        }

        private object ParseNumber()
        {
            string number = NextWord;

            if (number.IndexOf('.') == -1)
            {
                long parsedInt;
                long.TryParse(number, out parsedInt);
                return parsedInt;
            }

            double parsedDouble;
            double.TryParse(number, out parsedDouble);
            return parsedDouble;
        }

        private void EatWhitespace()
        {
            while (char.IsWhiteSpace((char)json.Peek()))
            {
                json.Read();
            }
        }

        private char NextChar
        {
            get
            {
                return Convert.ToChar(json.Read());
            }
        }

        private TOKEN NextToken
        {
            get
            {
                EatWhitespace();

                if (json.Peek() == -1)
                {
                    return TOKEN.NONE;
                }

                switch ((char)json.Peek())
                {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        json.Read();
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        json.Read();
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        json.Read();
                        return TOKEN.COMMA;
                    case '"':
                        return TOKEN.STRING;
                    case ':':
                        return TOKEN.COLON;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        return TOKEN.NUMBER;
                }

                switch (NextWord)
                {
                    case "false":
                        return TOKEN.FALSE;
                    case "true":
                        return TOKEN.TRUE;
                    case "null":
                        return TOKEN.NULL;
                }

                return TOKEN.NONE;
            }
        }

        private string NextWord
        {
            get
            {
                StringBuilder word = new StringBuilder();

                while (!WordBreak.Contains(((char)json.Peek()).ToString()))
                {
                    word.Append(NextChar);

                    if (json.Peek() == -1)
                    {
                        break;
                    }
                }

                return word.ToString();
            }
        }

        private enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARED_OPEN,
            SQUARED_CLOSE,
            COLON,
            COMMA,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL
        };
    }

    private class StringReader : IDisposable
    {
        private string json;
        private int position;

        public StringReader(string jsonString)
        {
            this.json = jsonString;
            this.position = 0;
        }

        public void Dispose()
        {
            json = null;
            position = 0;
        }

        public int Peek()
        {
            if (position >= json.Length)
            {
                return -1;
            }
            return json[position];
        }

        public int Read()
        {
            if (position >= json.Length)
            {
                return -1;
            }
            return json[position++];
        }
    }
}
