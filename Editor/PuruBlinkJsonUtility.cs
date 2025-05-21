using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VRCFaceController
{
    public static class PuruBlinkJsonUtility
    {
        // JSON文字列から辞書を生成する
        public static Dictionary<string, string> DeserializeStringDictionary(string json)
        {
            var sr = new StringReader(json);
            var obj = ParseObject(sr);
            return CastDictionary(obj);
        }
        
        // オブジェクトをstring型の辞書に変換
        private static Dictionary<string, string> CastDictionary(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var pair in dict)
            {
                if (pair.Value is string value)
                {
                    result[pair.Key] = value;
                }
                else if (pair.Value != null)
                {
                    result[pair.Key] = pair.Value.ToString();
                }
            }
            
            return result;
        }
        
        // JSONオブジェクトをパース
        private static Dictionary<string, object> ParseObject(StringReader sr)
        {
            var dict = new Dictionary<string, object>();
            int c;
            
            // 最初の '{' を読み飛ばす
            c = sr.ReadToNonWhitespace();
            if (c != '{') throw new FormatException("JSONオブジェクトが '{' で始まっていません");
            
            while ((c = sr.ReadToNonWhitespace()) != -1)
            {
                // オブジェクト終了
                if (c == '}') return dict;
                
                // キーは必ず文字列
                if (c != '"') throw new FormatException("JSONのキーが文字列ではありません");
                
                // キーの解析
                string key = ParseString(sr);
                
                // キーと値の区切り文字 ':'
                c = sr.ReadToNonWhitespace();
                if (c != ':') throw new FormatException("キーと値の区切り文字 ':' がありません");
                
                // 値の解析
                object value = ParseValue(sr);
                dict[key] = value;
                
                // 次の要素があるかどうか
                c = sr.ReadToNonWhitespace();
                if (c == '}') return dict; // オブジェクト終了
                if (c != ',') throw new FormatException("要素の区切り文字 ',' または終了文字 '}' がありません");
            }
            
            throw new FormatException("JSONオブジェクトが閉じられていません");
        }
        
        // JSON値をパース
        private static object ParseValue(StringReader sr)
        {
            int c = sr.ReadToNonWhitespace();
            if (c == -1) throw new FormatException("予期せぬJSONの終わりです");
            
            switch (c)
            {
                case '"': // 文字列
                    return ParseString(sr);
                    
                case '{': // オブジェクト
                    sr.Read(); // '{' を読み飛ばす（既に読んでしまったので戻す）
                    return ParseObject(sr);
                    
                case '[': // 配列（ローカライズファイルでは使用しないが一応対応）
                    return ParseArray(sr);
                    
                case 't': // true
                    if (sr.Read() == 'r' && sr.Read() == 'u' && sr.Read() == 'e')
                        return true;
                    throw new FormatException("JSONの 'true' が不正です");
                    
                case 'f': // false
                    if (sr.Read() == 'a' && sr.Read() == 'l' && sr.Read() == 's' && sr.Read() == 'e')
                        return false;
                    throw new FormatException("JSONの 'false' が不正です");
                    
                case 'n': // null
                    if (sr.Read() == 'u' && sr.Read() == 'l' && sr.Read() == 'l')
                        return null;
                    throw new FormatException("JSONの 'null' が不正です");
                    
                default: // 数値
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return ParseNumber(sr, c);
                    }
                    throw new FormatException($"不正なJSON要素です: '{(char)c}'");
            }
        }
        
        // 文字列をパース
        private static string ParseString(StringReader sr)
        {
            var sb = new StringBuilder();
            
            // 最初の '"' は読み飛ばす
            int c;
            
            while ((c = sr.Read()) != -1)
            {
                if (c == '\\') // エスケープシーケンス
                {
                    c = sr.Read();
                    if (c == -1) throw new FormatException("不正なエスケープシーケンスです");
                    
                    switch (c)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u': // Unicode
                            var hex = new StringBuilder();
                            for (int i = 0; i < 4; i++)
                            {
                                c = sr.Read();
                                if (c == -1) throw new FormatException("不正なUnicodeエスケープシーケンスです");
                                hex.Append((char)c);
                            }
                            sb.Append((char)Convert.ToInt32(hex.ToString(), 16));
                            break;
                        default:
                            throw new FormatException($"不正なエスケープシーケンスです: '\\{(char)c}'");
                    }
                }
                else if (c == '"') // 文字列終了
                {
                    return sb.ToString();
                }
                else
                {
                    sb.Append((char)c);
                }
            }
            
            throw new FormatException("文字列が閉じられていません");
        }
        
        // 配列をパース
        private static List<object> ParseArray(StringReader sr)
        {
            var list = new List<object>();
            
            // 最初の '[' を読み飛ばす
            int c = sr.Read();
            
            // 空の配列
            c = sr.ReadToNonWhitespace();
            if (c == ']') return list;
            
            // 配列の要素をパース
            while (c != -1)
            {
                sr.Read(); // 読み飛ばしたものを戻す
                
                // 値の解析
                object value = ParseValue(sr);
                list.Add(value);
                
                // 次の要素があるかどうか
                c = sr.ReadToNonWhitespace();
                if (c == ']') return list; // 配列終了
                if (c != ',') throw new FormatException("要素の区切り文字 ',' または終了文字 ']' がありません");
                
                c = sr.ReadToNonWhitespace();
            }
            
            throw new FormatException("JSONの配列が閉じられていません");
        }
        
        // 数値をパース
        private static object ParseNumber(StringReader sr, int firstChar)
        {
            var sb = new StringBuilder();
            sb.Append((char)firstChar);
            
            int c;
            bool isFloat = false;
            
            while ((c = sr.Peek()) != -1)
            {
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                {
                    sr.Read(); // 読み込み
                    sb.Append((char)c);
                    
                    if (c == '.' || c == 'e' || c == 'E')
                    {
                        isFloat = true;
                    }
                }
                else
                {
                    break;
                }
            }
            
            string number = sb.ToString();
            
            if (isFloat)
            {
                if (float.TryParse(number, out float floatValue))
                {
                    return floatValue;
                }
            }
            else
            {
                if (int.TryParse(number, out int intValue))
                {
                    return intValue;
                }
            }
            
            throw new FormatException($"不正な数値形式です: '{number}'");
        }
        
        // 空白文字を読み飛ばして次の有効な文字を返す
        private static int ReadToNonWhitespace(this StringReader sr)
        {
            int c;
            while ((c = sr.Peek()) != -1)
            {
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    sr.Read(); // 空白文字を読み飛ばす
                }
                else
                {
                    return sr.Read(); // 有効な文字を返す
                }
            }
            return -1; // ファイル終端
        }
    }
}
