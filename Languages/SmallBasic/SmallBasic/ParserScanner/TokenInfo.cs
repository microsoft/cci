//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
namespace Microsoft.Cci.SmallBasic
{
    public class TokenInfo
    {
        int _start, _length;
        Token _token;
        TokenType _tokenType;

        /// <summary>
        /// Gets or sets the start character index of the token
        /// </summary>
        public int Start
        {
            get
            {
                return _start;
            }

            set
            {
                _start = value;
            }
        }

        /// <summary>
        /// Gets or sets the length of the token
        /// </summary>
        public int Length
        {
            get
            {
                return _length;
            }

            set
            {
                _length = value;
            }
        }

        /// <summary>
        /// Gets or sets the Token
        /// </summary>
        public Token Token
        {
            get
            {
                return _token;
            }

            set
            {
                _token = value;
            }
        }

        /// <summary>
        /// Gets or sets the TokenType
        /// </summary>
        public TokenType TokenType
        {
            get
            {
                return _tokenType;
            }

            set
            {
                _tokenType = value;
            }
        }
    }
}