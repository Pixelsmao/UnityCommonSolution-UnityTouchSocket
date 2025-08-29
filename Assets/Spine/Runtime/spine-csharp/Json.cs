/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Spine
{
    public static class Json
    {
        public static object Deserialize(TextReader text)
        {
            var parser = new SharpJson.JsonDecoder();
            parser.parseNumbersAsFloat = true;
            return parser.Decode(text.ReadToEnd());
        }
    }
}

/**
 * Copyright (c) 2016 Adriano Tinoco d'Oliveira Rezende
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/news/view/14605/14863/how-do-i-write-my-own-parser-(for-json).html
 *
 * Changes made:
 *
 * - Optimized parser speed (deserialize roughly near 3x faster than original)
 * - Added support to handle lexer/parser error messages with line numbers
 * - Added more fine grained control over type conversions during the parsing
 * - Refactory API (Separate Lexer code from Parser code and the Encoder from Decoder)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
 * OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace SharpJson
{
    internal class Lexer
    {
        public enum Token
        {
            None,
            Null,
            True,
            False,
            Colon,
            Comma,
            String,
            Number,
            CurlyOpen,
            CurlyClose,
            SquaredOpen,
            SquaredClose,
        };

        public bool hasError
        {
            get
            {
                return !this.success;
            }
        }

        public int lineNumber
        {
            get;
            private set;
        }

        public bool parseNumbersAsFloat
        {
            get;
            set;
        }

        private readonly char[] json;
        private int index = 0;
        private bool success = true;
        private readonly char[] stringBuffer = new char[4096];

        public Lexer(string text)
        {
            this.Reset();

            this.json = text.ToCharArray();
            this.parseNumbersAsFloat = false;
        }

        public void Reset()
        {
            this.index = 0;
            this.lineNumber = 1;
            this.success = true;
        }

        public string ParseString()
        {
            var idx = 0;
            StringBuilder builder = null;

            this.SkipWhiteSpaces();

            // "
            var c = this.json[this.index++];

            var failed = false;
            var complete = false;

            while (!complete && !failed)
            {
                if (this.index == this.json.Length)
                    break;

                c = this.json[this.index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {
                    if (this.index == this.json.Length)
                        break;

                    c = this.json[this.index++];

                    switch (c)
                    {
                        case '"':
                            this.stringBuffer[idx++] = '"';
                            break;
                        case '\\':
                            this.stringBuffer[idx++] = '\\';
                            break;
                        case '/':
                            this.stringBuffer[idx++] = '/';
                            break;
                        case 'b':
                            this.stringBuffer[idx++] = '\b';
                            break;
                        case 'f':
                            this.stringBuffer[idx++] = '\f';
                            break;
                        case 'n':
                            this.stringBuffer[idx++] = '\n';
                            break;
                        case 'r':
                            this.stringBuffer[idx++] = '\r';
                            break;
                        case 't':
                            this.stringBuffer[idx++] = '\t';
                            break;
                        case 'u':
                            var remainingLength = this.json.Length - this.index;
                            if (remainingLength >= 4)
                            {
                                var hex = new string(this.json, this.index, 4);

                                // XXX: handle UTF
                                this.stringBuffer[idx++] = (char)Convert.ToInt32(hex, 16);

                                // skip 4 chars
                                this.index += 4;
                            }
                            else
                            {
                                failed = true;
                            }
                            break;
                    }
                }
                else
                {
                    this.stringBuffer[idx++] = c;
                }

                if (idx >= this.stringBuffer.Length)
                {
                    if (builder == null)
                        builder = new StringBuilder();

                    builder.Append(this.stringBuffer, 0, idx);
                    idx = 0;
                }
            }

            if (!complete)
            {
                this.success = false;
                return null;
            }

            if (builder != null)
                return builder.ToString();
            else
                return new string(this.stringBuffer, 0, idx);
        }

        private string GetNumberString()
        {
            this.SkipWhiteSpaces();

            var lastIndex = this.GetLastIndexOfNumber(this.index);
            var charLength = (lastIndex - this.index) + 1;

            var result = new string(this.json, this.index, charLength);

            this.index = lastIndex + 1;

            return result;
        }

        public float ParseFloatNumber()
        {
            var str = this.GetNumberString();

            if (!float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return 0;

            return number;
        }

        public double ParseDoubleNumber()
        {
            var str = this.GetNumberString();

            if (!double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                return 0;

            return number;
        }

        private int GetLastIndexOfNumber(int index)
        {
            int lastIndex;

            for (lastIndex = index; lastIndex < this.json.Length; lastIndex++)
            {
                var ch = this.json[lastIndex];

                if ((ch < '0' || ch > '9') && ch != '+' && ch != '-'
                    && ch != '.' && ch != 'e' && ch != 'E')
                    break;
            }

            return lastIndex - 1;
        }

        private void SkipWhiteSpaces()
        {
            for (; this.index < this.json.Length; this.index++)
            {
                var ch = this.json[this.index];

                if (ch == '\n')
                    this.lineNumber++;

                if (!char.IsWhiteSpace(this.json[this.index]))
                    break;
            }
        }

        public Token LookAhead()
        {
            this.SkipWhiteSpaces();

            var savedIndex = this.index;
            return NextToken(this.json, ref savedIndex);
        }

        public Token NextToken()
        {
            this.SkipWhiteSpaces();
            return NextToken(this.json, ref this.index);
        }

        private static Token NextToken(char[] json, ref int index)
        {
            if (index == json.Length)
                return Token.None;

            var c = json[index++];

            switch (c)
            {
                case '{':
                    return Token.CurlyOpen;
                case '}':
                    return Token.CurlyClose;
                case '[':
                    return Token.SquaredOpen;
                case ']':
                    return Token.SquaredClose;
                case ',':
                    return Token.Comma;
                case '"':
                    return Token.String;
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
                    return Token.Number;
                case ':':
                    return Token.Colon;
            }

            index--;

            var remainingLength = json.Length - index;

            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' &&
                    json[index + 1] == 'a' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 's' &&
                    json[index + 4] == 'e')
                {
                    index += 5;
                    return Token.False;
                }
            }

            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' &&
                    json[index + 1] == 'r' &&
                    json[index + 2] == 'u' &&
                    json[index + 3] == 'e')
                {
                    index += 4;
                    return Token.True;
                }
            }

            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' &&
                    json[index + 1] == 'u' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 'l')
                {
                    index += 4;
                    return Token.Null;
                }
            }

            return Token.None;
        }
    }

    public class JsonDecoder
    {
        public string errorMessage
        {
            get;
            private set;
        }

        public bool parseNumbersAsFloat
        {
            get;
            set;
        }

        private Lexer lexer;

        public JsonDecoder()
        {
            this.errorMessage = null;
            this.parseNumbersAsFloat = false;
        }

        public object Decode(string text)
        {
            this.errorMessage = null;

            this.lexer = new Lexer(text);
            this.lexer.parseNumbersAsFloat = this.parseNumbersAsFloat;

            return this.ParseValue();
        }

        public static object DecodeText(string text)
        {
            var builder = new JsonDecoder();
            return builder.Decode(text);
        }

        private IDictionary<string, object> ParseObject()
        {
            var table = new Dictionary<string, object>();

            // {
            this.lexer.NextToken();

            while (true)
            {
                var token = this.lexer.LookAhead();

                switch (token)
                {
                    case Lexer.Token.None:
                        this.TriggerError("Invalid token");
                        return null;
                    case Lexer.Token.Comma:
                        this.lexer.NextToken();
                        break;
                    case Lexer.Token.CurlyClose:
                        this.lexer.NextToken();
                        return table;
                    default:
                        // name
                        var name = this.EvalLexer(this.lexer.ParseString());

                        if (this.errorMessage != null)
                            return null;

                        // :
                        token = this.lexer.NextToken();

                        if (token != Lexer.Token.Colon)
                        {
                            this.TriggerError("Invalid token; expected ':'");
                            return null;
                        }

                        // value
                        var value = this.ParseValue();

                        if (this.errorMessage != null)
                            return null;

                        table[name] = value;
                        break;
                }
            }

            //return null; // Unreachable code
        }

        private IList<object> ParseArray()
        {
            var array = new List<object>();

            // [
            this.lexer.NextToken();

            while (true)
            {
                var token = this.lexer.LookAhead();

                switch (token)
                {
                    case Lexer.Token.None:
                        this.TriggerError("Invalid token");
                        return null;
                    case Lexer.Token.Comma:
                        this.lexer.NextToken();
                        break;
                    case Lexer.Token.SquaredClose:
                        this.lexer.NextToken();
                        return array;
                    default:
                        var value = this.ParseValue();

                        if (this.errorMessage != null)
                            return null;

                        array.Add(value);
                        break;
                }
            }

            //return null; // Unreachable code
        }

        private object ParseValue()
        {
            switch (this.lexer.LookAhead())
            {
                case Lexer.Token.String:
                    return this.EvalLexer(this.lexer.ParseString());
                case Lexer.Token.Number:
                    if (this.parseNumbersAsFloat)
                        return this.EvalLexer(this.lexer.ParseFloatNumber());
                    else
                        return this.EvalLexer(this.lexer.ParseDoubleNumber());
                case Lexer.Token.CurlyOpen:
                    return this.ParseObject();
                case Lexer.Token.SquaredOpen:
                    return this.ParseArray();
                case Lexer.Token.True:
                    this.lexer.NextToken();
                    return true;
                case Lexer.Token.False:
                    this.lexer.NextToken();
                    return false;
                case Lexer.Token.Null:
                    this.lexer.NextToken();
                    return null;
                case Lexer.Token.None:
                    break;
            }

            this.TriggerError("Unable to parse value");
            return null;
        }

        private void TriggerError(string message)
        {
            this.errorMessage = string.Format("Error: '{0}' at line {1}",
                                         message, this.lexer.lineNumber);
        }

        private T EvalLexer<T>(T value)
        {
            if (this.lexer.hasError)
                this.TriggerError("Lexical error ocurred");

            return value;
        }
    }
}
