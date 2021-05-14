/*
Original code by Lee Thomason (www.grinninglizard.com)

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any
damages arising from the use of this software.

Permission is granted to anyone to use this software for any
purpose, including commercial applications, and to alter it and
redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must
not claim that you wrote the original software. If you use this
software in a product, an acknowledgment in the product documentation
would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and
must not be misrepresented as being the original software.

3. This notice may not be removed or altered from any source
distribution.
*/

#include "tinyxml2.h"

#include <new>		// yes, this one new style header, is in the Android SDK.
#if defined(ANDROID_NDK) || defined(__BORLANDC__) || defined(__QNXNTO__)
#   include <stddef.h>
#   include <stdarg.h>
#else
#   include <cstddef>
#   include <cstdarg>
#endif

#if defined(_MSC_VER) && (_MSC_VER >= 1400 ) && (!defined WINCE)
	// Microsoft Visual Studio, version 2005 and higher. Not WinCE.
	/*int _snprintf_s(
	   char *buffer,
	   size_t sizeOfBuffer,
	   size_t count,
	   const char *format [,
		  argument] ...
	);*/
	static inline int TIXML_SNPRINTF( char* buffer, size_t size, const char* format, ... )
	{
		va_list va;
		va_start( va, format );
		int result = vsnprintf_s( buffer, size, _TRUNCATE, format, va );
		va_end( va );
		return result;
	}

	static inline int TIXML_VSNPRINTF( char* buffer, size_t size, const char* format, va_list va )
	{
		int result = vsnprintf_s( buffer, size, _TRUNCATE, format, va );
		return result;
	}

	#define TIXML_VSCPRINTF	_vscprintf
	#define TIXML_SSCANF	sscanf_s
#elif defined _MSC_VER
	// Microsoft Visual Studio 2003 and earlier or WinCE
	#define TIXML_SNPRINTF	_snprintf
	#define TIXML_VSNPRINTF _vsnprintf
	#define TIXML_SSCANF	sscanf
	#if (_MSC_VER < 1400 ) && (!defined WINCE)
		// Microsoft Visual Studio 2003 and not WinCE.
		#define TIXML_VSCPRINTF   _vscprintf // VS2003's C runtime has this, but VC6 C runtime or WinCE SDK doesn't have.
	#else
		// Microsoft Visual Studio 2003 and earlier or WinCE.
		static inline int TIXML_VSCPRINTF( const char* format, va_list va )
		{
			int len = 512;
			for (;;) {
				len = len*2;
				char* str = new char[len]();
				const int required = _vsnprintf(str, len, format, va);
				delete[] str;
				if ( required != -1 ) {
					TIXMLASSERT( required >= 0 );
					len = required;
					break;
				}
			}
			TIXMLASSERT( len >= 0 );
			return len;
		}
	#endif
#else
	// GCC version 3 and higher
	//#warning( "Using sn* functions." )
	#define TIXML_SNPRINTF	snprintf
	#define TIXML_VSNPRINTF	vsnprintf
	static inline int TIXML_VSCPRINTF( const char* format, va_list va )
	{
		int len = vsnprintf( 0, 0, format, va );
		TIXMLASSERT( len >= 0 );
		return len;
	}
	#define TIXML_SSCANF   sscanf
#endif


static const char LINE_FEED				= (char)0x0a;			// all line endings are normalized to LF
static const char LF = LINE_FEED;
static const char CARRIAGE_RETURN		= (char)0x0d;			// CR gets filtered out
static const char CR = CARRIAGE_RETURN;
static const char SINGLE_QUOTE			= '\'';
static const char DOUBLE_QUOTE			= '\"';

// Bunch of unicode info at:
//		http://www.unicode.org/faq/utf_bom.html
//	ef bb bf (Microsoft "lead bytes") - designates UTF-8

static const unsigned char TIXML_UTF_LEAD_0 = 0xefU;
static const unsigned char TIXML_UTF_LEAD_1 = 0xbbU;
static const unsigned char TIXML_UTF_LEAD_2 = 0xbfU;

namespace tinyxml2
{

struct Entity {
    const char* pattern;
    int length;
    char value;
};

static const int NUM_ENTITIES = 5;
static const Entity entities[NUM_ENTITIES] = {
    { "quot", 4,	DOUBLE_QUOTE },
    { "amp", 3,		'&'  },
 //   { "apos", 4,	SINGLE_QUOTE },
    { "lt",	2, 		'<'	 },
    { "gt",	2,		'>'	 }
};


StrPair::~StrPair()
{
    Reset();
}


void StrPair::TransferTo( StrPair* other )
{
    if ( this == other ) {
        return;
    }
    // This in effect implements the assignment operator by "moving"
    // ownership (as in auto_ptr).

    TIXMLASSERT( other->_flags == 0 );
    TIXMLASSERT( other->_start == 0 );
    TIXMLASSERT( other->_end == 0 );

    other->Reset();

    other->_flags = _flags;
    other->_start = _start;
    other->_end = _end;

    _flags = 0;
    _start = 0;
    _end = 0;
}

void StrPair::Reset()
{
    if ( _flags & NEEDS_DELETE ) {
        delete [] _start;
    }
    _flags = 0;
    _start = 0;
    _end = 0;
}


void StrPair::SetStr( const char* str, int flags )
{
    TIXMLASSERT( str );
    Reset();
    size_t len = strlen( str );
    TIXMLASSERT( _start == 0 );
    _start = new char[ len+1 ];
    memcpy( _start, str, len+1 );
    _end = _start + len;
    _flags = flags | NEEDS_DELETE;
}


char* StrPair::ParseText( char* p, const char* endTag, int strFlags )
{
    TIXMLASSERT( endTag && *endTag );

    char* start = p;
    char  endChar = *endTag;
    size_t length = strlen( endTag );

    // Inner loop of text parsing.
    while ( *p ) {
        if ( *p == endChar && strncmp( p, endTag, length ) == 0 ) {
            Set( start, p, strFlags );
            return p + length;
        }
        ++p;
    }
    return 0;
}


char* StrPair::ParseName( char* p )
{
    if ( !p || !(*p) ) {
        return 0;
    }
    if ( !XmlUtil::IsNameStartChar( *p ) ) {
        return 0;
    }

    char* const start = p;
    ++p;
    while ( *p && XmlUtil::IsNameChar( *p ) ) {
        ++p;
    }

    Set( start, p, 0 );
    return p;
}


void StrPair::CollapseWhitespace()
{
    // Adjusting _start would cause undefined behavior on delete[]
    TIXMLASSERT( ( _flags & NEEDS_DELETE ) == 0 );
    // Trim leading space.
    _start = XmlUtil::SkipWhiteSpace( _start );

    if ( *_start ) {
        char* p = _start;	// the read pointer
        char* q = _start;	// the write pointer

        while( *p ) {
            if ( XmlUtil::IsWhiteSpace( *p )) {
                p = XmlUtil::SkipWhiteSpace( p );
                if ( *p == 0 ) {
                    break;    // don't write to q; this trims the trailing space.
                }
                *q = ' ';
                ++q;
            }
            *q = *p;
            ++q;
            ++p;
        }
        *q = 0;
    }
}


const char* StrPair::GetStr()
{
    TIXMLASSERT( _start );
    TIXMLASSERT( _end );
    if ( _flags & NEEDS_FLUSH ) {
        *_end = 0;
        _flags ^= NEEDS_FLUSH;

        if ( _flags ) {
            char* p = _start;	// the read pointer
            char* q = _start;	// the write pointer

			// Trim front and back spaces
			_start = XmlUtil::SkipWhiteSpace(_start);
			_end = XmlUtil::SkipWhiteSpace(_end);

            while( p < _end ) {
                if ( (_flags & NEEDS_NEWLINE_NORMALIZATION) && *p == CR ) {
                    // CR-LF pair becomes LF
                    // CR alone becomes LF
                    // LF-CR becomes LF
                    if ( *(p+1) == LF ) {
                        p += 2;
                    }
                    else {
                        ++p;
                    }
                    *q++ = LF;
                }
                else if ( (_flags & NEEDS_NEWLINE_NORMALIZATION) && *p == LF ) {
                    if ( *(p+1) == CR ) {
                        p += 2;
                    }
                    else {
                        ++p;
                    }
                    *q++ = LF;
                }
                else if ( (_flags & NEEDS_ENTITY_PROCESSING) && *p == '&' ) {
                    // Entities handled by tinyXML2:
                    // - special entities in the entity table [in/out]
                    // - numeric character reference [in]
                    //   &#20013; or &#x4e2d;

                    if ( *(p+1) == '#' ) {
                        const int buflen = 10;
                        char buf[buflen] = { 0 };
                        int len = 0;
                        char* adjusted = const_cast<char*>( XmlUtil::GetCharacterRef( p, buf, &len ) );
                        if ( adjusted == 0 ) {
                            *q = *p;
                            ++p;
                            ++q;
                        }
                        else {
                            TIXMLASSERT( 0 <= len && len <= buflen );
                            TIXMLASSERT( q + len <= adjusted );
                            p = adjusted;
                            memcpy( q, buf, len );
                            q += len;
                        }
                    }
                    else {
                        bool entityFound = false;
                        for( int i = 0; i < NUM_ENTITIES; ++i ) {
                            const Entity& entity = entities[i];
                            if ( strncmp( p + 1, entity.pattern, entity.length ) == 0
                                    && *( p + entity.length + 1 ) == ';' ) {
                                // Found an entity - convert.
                                *q = entity.value;
                                ++q;
                                p += entity.length + 2;
                                entityFound = true;
                                break;
                            }
                        }
                        if ( !entityFound ) {
                            // fixme: treat as error?
                            ++p;
                            ++q;
                        }
                    }
                }
                else {
                    *q = *p;
                    ++p;
                    ++q;
                }
            }
            *q = 0;
        }
        // The loop below has plenty going on, and this
        // is a less useful mode. Break it out.
        if ( _flags & NEEDS_WHITESPACE_COLLAPSING ) {
            CollapseWhitespace();
        }
        _flags = (_flags & NEEDS_DELETE);
    }
    TIXMLASSERT( _start );
    return _start;
}




// --------- XmlUtil ----------- //

const char* XmlUtil::ReadBOM( const char* p, bool* bom )
{
    TIXMLASSERT( p );
    TIXMLASSERT( bom );
    *bom = false;
    const unsigned char* pu = reinterpret_cast<const unsigned char*>(p);
    // Check for BOM:
    if (    *(pu+0) == TIXML_UTF_LEAD_0
            && *(pu+1) == TIXML_UTF_LEAD_1
            && *(pu+2) == TIXML_UTF_LEAD_2 ) {
        *bom = true;
        p += 3;
    }
    TIXMLASSERT( p );
    return p;
}


void XmlUtil::ConvertUTF32ToUTF8( unsigned long input, char* output, int* length )
{
    const unsigned long BYTE_MASK = 0xBF;
    const unsigned long BYTE_MARK = 0x80;
    const unsigned long FIRST_BYTE_MARK[7] = { 0x00, 0x00, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC };

    if (input < 0x80) {
        *length = 1;
    }
    else if ( input < 0x800 ) {
        *length = 2;
    }
    else if ( input < 0x10000 ) {
        *length = 3;
    }
    else if ( input < 0x200000 ) {
        *length = 4;
    }
    else {
        *length = 0;    // This code won't convert this correctly anyway.
        return;
    }

    output += *length;

    // Scary scary fall throughs.
    switch (*length) {
        case 4:
            --output;
            *output = (char)((input | BYTE_MARK) & BYTE_MASK);
            input >>= 6;
        case 3:
            --output;
            *output = (char)((input | BYTE_MARK) & BYTE_MASK);
            input >>= 6;
        case 2:
            --output;
            *output = (char)((input | BYTE_MARK) & BYTE_MASK);
            input >>= 6;
        case 1:
            --output;
            *output = (char)(input | FIRST_BYTE_MARK[*length]);
            break;
        default:
            TIXMLASSERT( false );
    }
}


const char* XmlUtil::GetCharacterRef( const char* p, char* value, int* length )
{
    // Presume an entity, and pull it out.
    *length = 0;

    if ( *(p+1) == '#' && *(p+2) ) {
        unsigned long ucs = 0;
        TIXMLASSERT( sizeof( ucs ) >= 4 );
        ptrdiff_t delta = 0;
        unsigned mult = 1;
        static const char SEMICOLON = ';';

        if ( *(p+2) == 'x' ) {
            // Hexadecimal.
            const char* q = p+3;
            if ( !(*q) ) {
                return 0;
            }

            q = strchr( q, SEMICOLON );

            if ( !q ) {
                return 0;
            }
            TIXMLASSERT( *q == SEMICOLON );

            delta = q-p;
            --q;

            while ( *q != 'x' ) {
                unsigned int digit = 0;

                if ( *q >= '0' && *q <= '9' ) {
                    digit = *q - '0';
                }
                else if ( *q >= 'a' && *q <= 'f' ) {
                    digit = *q - 'a' + 10;
                }
                else if ( *q >= 'A' && *q <= 'F' ) {
                    digit = *q - 'A' + 10;
                }
                else {
                    return 0;
                }
                TIXMLASSERT( digit < 16 );
                TIXMLASSERT( digit == 0 || mult <= UINT_MAX / digit );
                const unsigned int digitScaled = mult * digit;
                TIXMLASSERT( ucs <= ULONG_MAX - digitScaled );
                ucs += digitScaled;
                TIXMLASSERT( mult <= UINT_MAX / 16 );
                mult *= 16;
                --q;
            }
        }
        else {
            // Decimal.
            const char* q = p+2;
            if ( !(*q) ) {
                return 0;
            }

            q = strchr( q, SEMICOLON );

            if ( !q ) {
                return 0;
            }
            TIXMLASSERT( *q == SEMICOLON );

            delta = q-p;
            --q;

            while ( *q != '#' ) {
                if ( *q >= '0' && *q <= '9' ) {
                    const unsigned int digit = *q - '0';
                    TIXMLASSERT( digit < 10 );
                    TIXMLASSERT( digit == 0 || mult <= UINT_MAX / digit );
                    const unsigned int digitScaled = mult * digit;
                    TIXMLASSERT( ucs <= ULONG_MAX - digitScaled );
                    ucs += digitScaled;
                }
                else {
                    return 0;
                }
                TIXMLASSERT( mult <= UINT_MAX / 10 );
                mult *= 10;
                --q;
            }
        }
        // convert the UCS to UTF-8
        ConvertUTF32ToUTF8( ucs, value, length );
        return p + delta + 1;
    }
    return p+1;
}


void XmlUtil::ToStr( int v, char* buffer, int bufferSize )
{
    TIXML_SNPRINTF( buffer, bufferSize, "%d", v );
}


void XmlUtil::ToStr( unsigned v, char* buffer, int bufferSize )
{
    TIXML_SNPRINTF( buffer, bufferSize, "%u", v );
}


void XmlUtil::ToStr( bool v, char* buffer, int bufferSize )
{
    TIXML_SNPRINTF( buffer, bufferSize, "%d", v ? 1 : 0 );
}

/*
	ToStr() of a number is a very tricky topic.
	https://github.com/leethomason/tinyxml2/issues/106
*/
void XmlUtil::ToStr( float v, char* buffer, int bufferSize )
{
    TIXML_SNPRINTF( buffer, bufferSize, "%.8g", v );
}


void XmlUtil::ToStr( double v, char* buffer, int bufferSize )
{
    TIXML_SNPRINTF( buffer, bufferSize, "%.17g", v );
}


bool XmlUtil::ToInt( const char* str, int* value )
{
    if ( TIXML_SSCANF( str, "%d", value ) == 1 ) {
        return true;
    }
    return false;
}

bool XmlUtil::ToUnsigned( const char* str, unsigned *value )
{
    if ( TIXML_SSCANF( str, "%u", value ) == 1 ) {
        return true;
    }
    return false;
}

bool XmlUtil::ToBool( const char* str, bool* value )
{
    int ival = 0;
    if ( ToInt( str, &ival )) {
        *value = (ival==0) ? false : true;
        return true;
    }
    if ( StringEqual( str, "true" ) ) {
        *value = true;
        return true;
    }
    else if ( StringEqual( str, "false" ) ) {
        *value = false;
        return true;
    }
    return false;
}


bool XmlUtil::ToFloat( const char* str, float* value )
{
    if ( TIXML_SSCANF( str, "%f", value ) == 1 ) {
        return true;
    }
    return false;
}

bool XmlUtil::ToDouble( const char* str, double* value )
{
    if ( TIXML_SSCANF( str, "%lf", value ) == 1 ) {
        return true;
    }
    return false;
}

bool XmlUtil::_icase = true;


char* XmlDocument::Identify( char* p, XmlNode** node )
{
    TIXMLASSERT( node );
    TIXMLASSERT( p );
    char* const start = p;
    p = XmlUtil::SkipWhiteSpace( p );
    if( !*p ) {
        *node = 0;
        TIXMLASSERT( p );
        return p;
    }

    // These strings define the matching patterns:
    static const char* xmlHeader		= { "<?" };
    static const char* commentHeader	= { "<!--" };
    static const char* cdataHeader		= { "<![CDATA[" };
    static const char* dtdHeader		= { "<!" };
    static const char* elementHeader	= { "<" };	// and a header for everything else; check last.

    static const int xmlHeaderLen		= 2;
    static const int commentHeaderLen	= 4;
    static const int cdataHeaderLen		= 9;
    static const int dtdHeaderLen		= 2;
    static const int elementHeaderLen	= 1;

    TIXMLASSERT( sizeof( XmlComment ) == sizeof( XmlUnknown ) );		// use same memory pool
    TIXMLASSERT( sizeof( XmlComment ) == sizeof( XmlDeclaration ) );	// use same memory pool
    XmlNode* returnNode = 0;
    if ( XmlUtil::StringEqual( p, xmlHeader, xmlHeaderLen ) ) {
        TIXMLASSERT( sizeof( XmlDeclaration ) == _commentPool.ItemSize() );
        returnNode = new (_commentPool.Alloc()) XmlDeclaration( this );
        returnNode->_memPool = &_commentPool;
        p += xmlHeaderLen;
    }
    else if ( XmlUtil::StringEqual( p, commentHeader, commentHeaderLen ) ) {
        TIXMLASSERT( sizeof( XmlComment ) == _commentPool.ItemSize() );
        returnNode = new (_commentPool.Alloc()) XmlComment( this );
        returnNode->_memPool = &_commentPool;
        p += commentHeaderLen;
    }
    else if ( XmlUtil::StringEqual( p, cdataHeader, cdataHeaderLen ) ) {
        TIXMLASSERT( sizeof( XmlText ) == _textPool.ItemSize() );
        XmlText* text = new (_textPool.Alloc()) XmlText( this );
        returnNode = text;
        returnNode->_memPool = &_textPool;
        p += cdataHeaderLen;
        text->SetCData( true );
    }
    else if ( XmlUtil::StringEqual( p, dtdHeader, dtdHeaderLen ) ) {
        TIXMLASSERT( sizeof( XmlUnknown ) == _commentPool.ItemSize() );
        returnNode = new (_commentPool.Alloc()) XmlUnknown( this );
        returnNode->_memPool = &_commentPool;
        p += dtdHeaderLen;
    }
    else if ( XmlUtil::StringEqual( p, elementHeader, elementHeaderLen ) ) {
        TIXMLASSERT( sizeof( XmlElement ) == _elementPool.ItemSize() );
        returnNode = new (_elementPool.Alloc()) XmlElement( this );
        returnNode->_memPool = &_elementPool;
        p += elementHeaderLen;
    }
    else {
        TIXMLASSERT( sizeof( XmlText ) == _textPool.ItemSize() );
        returnNode = new (_textPool.Alloc()) XmlText( this );
        returnNode->_memPool = &_textPool;
        p = start;	// Back it up, all the text counts.
    }

    TIXMLASSERT( returnNode );
    TIXMLASSERT( p );
    *node = returnNode;
    return p;
}


bool XmlDocument::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    if ( visitor->VisitEnter( *this ) ) {
        for ( const XmlNode* node=FirstChild(); node; node=node->NextSibling() ) {
            if ( !node->Accept( visitor ) ) {
                break;
            }
        }
    }
    return visitor->VisitExit( *this );
}


// --------- XmlNode ----------- //

XmlNode::XmlNode( XmlDocument* doc ) :
    _document( doc ),
    _parent( 0 ),
    _firstChild( 0 ), _lastChild( 0 ),
    _prev( 0 ), _next( 0 ),
    _memPool( 0 )
{
}


XmlNode::~XmlNode()
{
    DeleteChildren();
    if ( _parent ) {
        _parent->Unlink( this );
    }
}

const char* XmlNode::Value() const 
{
    // Catch an edge case: XmlDocuments don't have a a Value. Carefully return nullptr.
    if ( this->ToDocument() )
        return 0;
    return _value.GetStr();
}

void XmlNode::SetValue( const char* str, bool staticMem )
{
    if ( staticMem ) {
        _value.SetInternedStr( str );
    }
    else {
        _value.SetStr( str );
    }
}


void XmlNode::DeleteChildren()
{
    while( _firstChild ) {
        TIXMLASSERT( _lastChild );
        TIXMLASSERT( _firstChild->_document == _document );
        XmlNode* node = _firstChild;
        Unlink( node );

        DeleteNode( node );
    }
    _firstChild = _lastChild = 0;
}


void XmlNode::Unlink( XmlNode* child )
{
    TIXMLASSERT( child );
    TIXMLASSERT( child->_document == _document );
    TIXMLASSERT( child->_parent == this );
    if ( child == _firstChild ) {
        _firstChild = _firstChild->_next;
    }
    if ( child == _lastChild ) {
        _lastChild = _lastChild->_prev;
    }

    if ( child->_prev ) {
        child->_prev->_next = child->_next;
    }
    if ( child->_next ) {
        child->_next->_prev = child->_prev;
    }
	child->_parent = 0;
}


void XmlNode::DeleteChild( XmlNode* node )
{
    TIXMLASSERT( node );
    TIXMLASSERT( node->_document == _document );
    TIXMLASSERT( node->_parent == this );
    Unlink( node );
    DeleteNode( node );
}


XmlNode* XmlNode::InsertEndChild( XmlNode* addThis )
{
    TIXMLASSERT( addThis );
    if ( addThis->_document != _document ) {
        TIXMLASSERT( false );
        return 0;
    }
    InsertChildPreamble( addThis );

    if ( _lastChild ) {
        TIXMLASSERT( _firstChild );
        TIXMLASSERT( _lastChild->_next == 0 );
        _lastChild->_next = addThis;
        addThis->_prev = _lastChild;
        _lastChild = addThis;

        addThis->_next = 0;
    }
    else {
        TIXMLASSERT( _firstChild == 0 );
        _firstChild = _lastChild = addThis;

        addThis->_prev = 0;
        addThis->_next = 0;
    }
    addThis->_parent = this;
    return addThis;
}


XmlNode* XmlNode::InsertFirstChild( XmlNode* addThis )
{
    TIXMLASSERT( addThis );
    if ( addThis->_document != _document ) {
        TIXMLASSERT( false );
        return 0;
    }
    InsertChildPreamble( addThis );

    if ( _firstChild ) {
        TIXMLASSERT( _lastChild );
        TIXMLASSERT( _firstChild->_prev == 0 );

        _firstChild->_prev = addThis;
        addThis->_next = _firstChild;
        _firstChild = addThis;

        addThis->_prev = 0;
    }
    else {
        TIXMLASSERT( _lastChild == 0 );
        _firstChild = _lastChild = addThis;

        addThis->_prev = 0;
        addThis->_next = 0;
    }
    addThis->_parent = this;
    return addThis;
}


XmlNode* XmlNode::InsertAfterChild( XmlNode* afterThis, XmlNode* addThis )
{
    TIXMLASSERT( addThis );
    if ( addThis->_document != _document ) {
        TIXMLASSERT( false );
        return 0;
    }

    TIXMLASSERT( afterThis );

    if ( afterThis->_parent != this ) {
        TIXMLASSERT( false );
        return 0;
    }

    if ( afterThis->_next == 0 ) {
        // The last node or the only node.
        return InsertEndChild( addThis );
    }
    InsertChildPreamble( addThis );
    addThis->_prev = afterThis;
    addThis->_next = afterThis->_next;
    afterThis->_next->_prev = addThis;
    afterThis->_next = addThis;
    addThis->_parent = this;
    return addThis;
}

sstring XmlNode::ToString()
{
	XmlPrinter p;
	this->Accept(&p);

	return sstring(p.CStr());
}


const XmlElement* XmlNode::FirstChildElement( const char* name ) const
{
    for( const XmlNode* node = _firstChild; node; node = node->_next ) {
        const XmlElement* element = node->ToElement();
        if ( element ) {
            if ( !name || XmlUtil::StringEqual( element->Name(), name ) ) {
                return element;
            }
        }
    }
    return 0;
}


const XmlElement* XmlNode::LastChildElement( const char* name ) const
{
    for( const XmlNode* node = _lastChild; node; node = node->_prev ) {
        const XmlElement* element = node->ToElement();
        if ( element ) {
            if ( !name || XmlUtil::StringEqual( element->Name(), name ) ) {
                return element;
            }
        }
    }
    return 0;
}


const XmlElement* XmlNode::NextSiblingElement( const char* name ) const
{
    for( const XmlNode* node = _next; node; node = node->_next ) {
        const XmlElement* element = node->ToElement();
        if ( element
                && (!name || XmlUtil::StringEqual( name, element->Name() ))) {
            return element;
        }
    }
    return 0;
}


const XmlElement* XmlNode::PreviousSiblingElement( const char* name ) const
{
    for( const XmlNode* node = _prev; node; node = node->_prev ) {
        const XmlElement* element = node->ToElement();
        if ( element
                && (!name || XmlUtil::StringEqual( name, element->Name() ))) {
            return element;
        }
    }
    return 0;
}


char* XmlNode::ParseDeep( char* p, StrPair* parentEnd )
{
    // This is a recursive method, but thinking about it "at the current level"
    // it is a pretty simple flat list:
    //		<foo/>
    //		<!-- comment -->
    //
    // With a special case:
    //		<foo>
    //		</foo>
    //		<!-- comment -->
    //
    // Where the closing element (/foo) *must* be the next thing after the opening
    // element, and the names must match. BUT the tricky bit is that the closing
    // element will be read by the child.
    //
    // 'endTag' is the end tag for this node, it is returned by a call to a child.
    // 'parentEnd' is the end tag for the parent, which is filled in and returned.

    while( p && *p ) {
        XmlNode* node = 0;

        p = _document->Identify( p, &node );
        if ( node == 0 ) {
            break;
        }

        StrPair endTag;
        p = node->ParseDeep( p, &endTag );
        if ( !p ) {
            DeleteNode( node );
            if ( !_document->Error() ) {
                _document->SetError( XML_ERROR_PARSING, 0, 0 );
            }
            break;
        }

        XmlDeclaration* decl = node->ToDeclaration();
        if ( decl ) {
                // A declaration can only be the first child of a document.
                // Set error, if document already has children.
                if ( !_document->NoChildren() ) {
                        _document->SetError( XML_ERROR_PARSING_DECLARATION, decl->Value(), 0);
                        DeleteNode( decl );
                        break;
                }
        }

        XmlElement* ele = node->ToElement();
        if ( ele ) {
            // We read the end tag. Return it to the parent.
            if ( ele->ClosingType() == XmlElement::CLOSING ) {
                if ( parentEnd ) {
                    ele->_value.TransferTo( parentEnd );
                }
                node->_memPool->SetTracked();   // created and then immediately deleted.
                DeleteNode( node );
                return p;
            }

            // Handle an end tag returned to this level.
            // And handle a bunch of annoying errors.
            bool mismatch = false;
            if ( endTag.Empty() ) {
                if ( ele->ClosingType() == XmlElement::OPEN ) {
                    mismatch = true;
                }
            }
            else {
                if ( ele->ClosingType() != XmlElement::OPEN ) {
                    mismatch = true;
                }
                else if ( !XmlUtil::StringEqual( endTag.GetStr(), ele->Name() ) ) {
                    mismatch = true;
                }
            }
            if ( mismatch ) {
                _document->SetError( XML_ERROR_MISMATCHED_ELEMENT, ele->Name(), 0 );
                DeleteNode( node );
                break;
            }
        }
        InsertEndChild( node );
    }
    return 0;
}

void XmlNode::DeleteNode( XmlNode* node )
{
    if ( node == 0 ) {
        return;
    }
    MemPool* pool = node->_memPool;
    node->~XmlNode();
    pool->Free( node );
}

void XmlNode::InsertChildPreamble( XmlNode* insertThis ) const
{
    TIXMLASSERT( insertThis );
    TIXMLASSERT( insertThis->_document == _document );

    if ( insertThis->_parent )
        insertThis->_parent->Unlink( insertThis );
    else
        insertThis->_memPool->SetTracked();
}

// --------- XmlText ---------- //
char* XmlText::ParseDeep( char* p, StrPair* )
{
    const char* start = p;
    if ( this->CData() ) {
        p = _value.ParseText( p, "]]>", StrPair::NEEDS_NEWLINE_NORMALIZATION );
        if ( !p ) {
            _document->SetError( XML_ERROR_PARSING_CDATA, start, 0 );
        }
        return p;
    }
    else {
        int flags = _document->ProcessEntities() ? StrPair::TEXT_ELEMENT : StrPair::TEXT_ELEMENT_LEAVE_ENTITIES;
        if ( _document->WhitespaceMode() == COLLAPSE_WHITESPACE ) {
            flags |= StrPair::NEEDS_WHITESPACE_COLLAPSING;
        }

        p = _value.ParseText( p, "<", flags );
        if ( p && *p ) {
            return p-1;
        }
        if ( !p ) {
            _document->SetError( XML_ERROR_PARSING_TEXT, start, 0 );
        }
    }
    return 0;
}


XmlNode* XmlText::ShallowClone( XmlDocument* doc ) const
{
    if ( !doc ) {
        doc = _document;
    }
    XmlText* text = doc->NewText( Value() );	// fixme: this will always allocate memory. Intern?
    text->SetCData( this->CData() );
    return text;
}


bool XmlText::ShallowEqual( const XmlNode* compare ) const
{
    const XmlText* text = compare->ToText();
    return ( text && XmlUtil::StringEqual( text->Value(), Value() ) );
}


bool XmlText::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    return visitor->Visit( *this );
}


// --------- XmlComment ---------- //

XmlComment::XmlComment( XmlDocument* doc ) : XmlNode( doc )
{
}


XmlComment::~XmlComment()
{
}


char* XmlComment::ParseDeep( char* p, StrPair* )
{
    // Comment parses as text.
    const char* start = p;
    p = _value.ParseText( p, "-->", StrPair::COMMENT );
    if ( p == 0 ) {
        _document->SetError( XML_ERROR_PARSING_COMMENT, start, 0 );
    }
    return p;
}


XmlNode* XmlComment::ShallowClone( XmlDocument* doc ) const
{
    if ( !doc ) {
        doc = _document;
    }
    XmlComment* comment = doc->NewComment( Value() );	// fixme: this will always allocate memory. Intern?
    return comment;
}


bool XmlComment::ShallowEqual( const XmlNode* compare ) const
{
    TIXMLASSERT( compare );
    const XmlComment* comment = compare->ToComment();
    return ( comment && XmlUtil::StringEqual( comment->Value(), Value() ));
}


bool XmlComment::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    return visitor->Visit( *this );
}


// --------- XmlDeclaration ---------- //

XmlDeclaration::XmlDeclaration( XmlDocument* doc ) : XmlNode( doc )
{
}


XmlDeclaration::~XmlDeclaration()
{
    //printf( "~XmlDeclaration\n" );
}


char* XmlDeclaration::ParseDeep( char* p, StrPair* )
{
    // Declaration parses as text.
    const char* start = p;
    p = _value.ParseText( p, "?>", StrPair::NEEDS_NEWLINE_NORMALIZATION );
    if ( p == 0 ) {
        _document->SetError( XML_ERROR_PARSING_DECLARATION, start, 0 );
    }
    return p;
}


XmlNode* XmlDeclaration::ShallowClone( XmlDocument* doc ) const
{
    if ( !doc ) {
        doc = _document;
    }
    XmlDeclaration* dec = doc->NewDeclaration( Value() );	// fixme: this will always allocate memory. Intern?
    return dec;
}


bool XmlDeclaration::ShallowEqual( const XmlNode* compare ) const
{
    TIXMLASSERT( compare );
    const XmlDeclaration* declaration = compare->ToDeclaration();
    return ( declaration && XmlUtil::StringEqual( declaration->Value(), Value() ));
}



bool XmlDeclaration::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    return visitor->Visit( *this );
}

// --------- XmlUnknown ---------- //

XmlUnknown::XmlUnknown( XmlDocument* doc ) : XmlNode( doc )
{
}


XmlUnknown::~XmlUnknown()
{
}


char* XmlUnknown::ParseDeep( char* p, StrPair* )
{
    // Unknown parses as text.
    const char* start = p;

    p = _value.ParseText( p, ">", StrPair::NEEDS_NEWLINE_NORMALIZATION );
    if ( !p ) {
        _document->SetError( XML_ERROR_PARSING_UNKNOWN, start, 0 );
    }
    return p;
}


XmlNode* XmlUnknown::ShallowClone( XmlDocument* doc ) const
{
    if ( !doc ) {
        doc = _document;
    }
    XmlUnknown* text = doc->NewUnknown( Value() );	// fixme: this will always allocate memory. Intern?
    return text;
}


bool XmlUnknown::ShallowEqual( const XmlNode* compare ) const
{
    TIXMLASSERT( compare );
    const XmlUnknown* unknown = compare->ToUnknown();
    return ( unknown && XmlUtil::StringEqual( unknown->Value(), Value() ));
}


bool XmlUnknown::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    return visitor->Visit( *this );
}

// --------- XmlAttribute ---------- //

const char* XmlAttribute::Name() const 
{
    return _name.GetStr();
}

const char* XmlAttribute::Value() const 
{
    return _value.GetStr();
}

char* XmlAttribute::ParseDeep( char* p, bool processEntities )
{
    // Parse using the name rules: bug fix, was using ParseText before
    p = _name.ParseName( p );
    if ( !p || !*p ) {
        return 0;
    }

    // Skip white space before =
    p = XmlUtil::SkipWhiteSpace( p );
    if ( *p != '=' ) {
        return 0;
    }

    ++p;	// move up to opening quote
    p = XmlUtil::SkipWhiteSpace( p );
    if ( *p != '\"' && *p != '\'' ) {
        return 0;
    }

    char endTag[2] = { *p, 0 };
    ++p;	// move past opening quote

    p = _value.ParseText( p, endTag, processEntities ? StrPair::ATTRIBUTE_VALUE : StrPair::ATTRIBUTE_VALUE_LEAVE_ENTITIES );
    return p;
}


void XmlAttribute::SetName( const char* n )
{
    _name.SetStr( n );
}


XMLError XmlAttribute::QueryIntValue( int* value ) const
{
    if ( XmlUtil::ToInt( Value(), value )) {
        return XML_NO_ERROR;
    }
    return XML_WRONG_ATTRIBUTE_TYPE;
}


XMLError XmlAttribute::QueryUnsignedValue( unsigned int* value ) const
{
    if ( XmlUtil::ToUnsigned( Value(), value )) {
        return XML_NO_ERROR;
    }
    return XML_WRONG_ATTRIBUTE_TYPE;
}


XMLError XmlAttribute::QueryBoolValue( bool* value ) const
{
    if ( XmlUtil::ToBool( Value(), value )) {
        return XML_NO_ERROR;
    }
    return XML_WRONG_ATTRIBUTE_TYPE;
}


XMLError XmlAttribute::QueryFloatValue( float* value ) const
{
    if ( XmlUtil::ToFloat( Value(), value )) {
        return XML_NO_ERROR;
    }
    return XML_WRONG_ATTRIBUTE_TYPE;
}


XMLError XmlAttribute::QueryDoubleValue( double* value ) const
{
    if ( XmlUtil::ToDouble( Value(), value )) {
        return XML_NO_ERROR;
    }
    return XML_WRONG_ATTRIBUTE_TYPE;
}


void XmlAttribute::SetAttribute( const char* v )
{
    _value.SetStr( v );
}


void XmlAttribute::SetAttribute( int v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    _value.SetStr( buf );
}


void XmlAttribute::SetAttribute( unsigned v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    _value.SetStr( buf );
}


void XmlAttribute::SetAttribute( bool v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    _value.SetStr( buf );
}

void XmlAttribute::SetAttribute( double v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    _value.SetStr( buf );
}

void XmlAttribute::SetAttribute( float v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    _value.SetStr( buf );
}


// --------- XmlElement ---------- //
XmlElement::XmlElement( XmlDocument* doc ) : XmlNode( doc ),
    _closingType( 0 ),
    _rootAttribute( 0 )
{
}


XmlElement::~XmlElement()
{
    while( _rootAttribute ) {
        XmlAttribute* next = _rootAttribute->_next;
        DeleteAttribute( _rootAttribute );
        _rootAttribute = next;
    }
}


const XmlAttribute* XmlElement::FindAttribute( const char* name ) const
{
    for( XmlAttribute* a = _rootAttribute; a; a = a->_next ) {
        if ( XmlUtil::StringEqual( a->Name(), name ) ) {
            return a;
        }
    }
    return 0;
}


const char* XmlElement::Attribute( const char* name, const char* value ) const
{
    const XmlAttribute* a = FindAttribute( name );
    if ( !a ) {
        return 0;
    }
    if ( !value || XmlUtil::StringEqual( a->Value(), value )) {
        return a->Value();
    }
    return 0;
}

const char* XmlElement::AttributeValue(const char* name)
{
	const XmlAttribute* attr = FindAttribute(name);
	if (attr)
		return attr->Value();
	
	return 0;
}

void XmlElement::AttributesMap(strmap& attrmap)
{
	attrmap.clear();

	for (XmlAttribute* a = _rootAttribute; a; a = a->_next) {
		
		sstring name = a->Name();

		if (XmlUtil::_icase)
		{
			for (unsigned int i = 0; i < name.length(); i++)
			{
				name[i] = tolower(name[i]);
			}
		}
		attrmap[name] = a->Value();
	}	
}

const char* XmlElement::GetText() const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        return FirstChild()->Value();
    }
    return 0;
}

const char* XmlElement::GetText(const char* aChildElement) const
{
	const XmlElement* child = FirstChildElement(aChildElement);
	if (child)
		return child->GetText();
	return "0";
}


void	XmlElement::SetText( const char* inText )
{
	if ( FirstChild() && FirstChild()->ToText() )
		FirstChild()->SetValue( inText );
	else {
		XmlText*	theText = GetDocument()->NewText( inText );
		InsertFirstChild( theText );
	}
}


void XmlElement::SetText( int v ) 
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    SetText( buf );
}


void XmlElement::SetText( unsigned v ) 
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    SetText( buf );
}


void XmlElement::SetText( bool v ) 
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    SetText( buf );
}


void XmlElement::SetText( float v ) 
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    SetText( buf );
}


void XmlElement::SetText( double v ) 
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    SetText( buf );
}


XMLError XmlElement::QueryIntText( int* ival ) const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        const char* t = FirstChild()->Value();
        if ( XmlUtil::ToInt( t, ival ) ) {
            return XML_SUCCESS;
        }
        return XML_CAN_NOT_CONVERT_TEXT;
    }
    return XML_NO_TEXT_NODE;
}


XMLError XmlElement::QueryUnsignedText( unsigned* uval ) const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        const char* t = FirstChild()->Value();
        if ( XmlUtil::ToUnsigned( t, uval ) ) {
            return XML_SUCCESS;
        }
        return XML_CAN_NOT_CONVERT_TEXT;
    }
    return XML_NO_TEXT_NODE;
}


XMLError XmlElement::QueryBoolText( bool* bval ) const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        const char* t = FirstChild()->Value();
        if ( XmlUtil::ToBool( t, bval ) ) {
            return XML_SUCCESS;
        }
        return XML_CAN_NOT_CONVERT_TEXT;
    }
    return XML_NO_TEXT_NODE;
}


XMLError XmlElement::QueryDoubleText( double* dval ) const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        const char* t = FirstChild()->Value();
        if ( XmlUtil::ToDouble( t, dval ) ) {
            return XML_SUCCESS;
        }
        return XML_CAN_NOT_CONVERT_TEXT;
    }
    return XML_NO_TEXT_NODE;
}


XMLError XmlElement::QueryFloatText( float* fval ) const
{
    if ( FirstChild() && FirstChild()->ToText() ) {
        const char* t = FirstChild()->Value();
        if ( XmlUtil::ToFloat( t, fval ) ) {
            return XML_SUCCESS;
        }
        return XML_CAN_NOT_CONVERT_TEXT;
    }
    return XML_NO_TEXT_NODE;
}



XmlAttribute* XmlElement::FindOrCreateAttribute( const char* name )
{
    XmlAttribute* last = 0;
    XmlAttribute* attrib = 0;
    for( attrib = _rootAttribute;
            attrib;
            last = attrib, attrib = attrib->_next ) {
        if ( XmlUtil::StringEqual( attrib->Name(), name ) ) {
            break;
        }
    }
    if ( !attrib ) {
        TIXMLASSERT( sizeof( XmlAttribute ) == _document->_attributePool.ItemSize() );
        attrib = new (_document->_attributePool.Alloc() ) XmlAttribute();
        attrib->_memPool = &_document->_attributePool;
        if ( last ) {
            last->_next = attrib;
        }
        else {
            _rootAttribute = attrib;
        }
        attrib->SetName( name );
        attrib->_memPool->SetTracked(); // always created and linked.
    }
    return attrib;
}


void XmlElement::DeleteAttribute( const char* name )
{
    XmlAttribute* prev = 0;
    for( XmlAttribute* a=_rootAttribute; a; a=a->_next ) {
        if ( XmlUtil::StringEqual( name, a->Name() ) ) {
            if ( prev ) {
                prev->_next = a->_next;
            }
            else {
                _rootAttribute = a->_next;
            }
            DeleteAttribute( a );
            break;
        }
        prev = a;
    }
}


char* XmlElement::ParseAttributes( char* p )
{
    const char* start = p;
    XmlAttribute* prevAttribute = 0;

    // Read the attributes.
    while( p ) {
        p = XmlUtil::SkipWhiteSpace( p );
        if ( !(*p) ) {
            _document->SetError( XML_ERROR_PARSING_ELEMENT, start, Name() );
            return 0;
        }

        // attribute.
        if (XmlUtil::IsNameStartChar( *p ) ) {
            TIXMLASSERT( sizeof( XmlAttribute ) == _document->_attributePool.ItemSize() );
            XmlAttribute* attrib = new (_document->_attributePool.Alloc() ) XmlAttribute();
            attrib->_memPool = &_document->_attributePool;
			attrib->_memPool->SetTracked();

            p = attrib->ParseDeep( p, _document->ProcessEntities() );
            if ( !p || Attribute( attrib->Name() ) ) {
                DeleteAttribute( attrib );
                _document->SetError( XML_ERROR_PARSING_ATTRIBUTE, start, p );
                return 0;
            }
            // There is a minor bug here: if the attribute in the source xml
            // document is duplicated, it will not be detected and the
            // attribute will be doubly added. However, tracking the 'prevAttribute'
            // avoids re-scanning the attribute list. Preferring performance for
            // now, may reconsider in the future.
            if ( prevAttribute ) {
                prevAttribute->_next = attrib;
            }
            else {
                _rootAttribute = attrib;
            }
            prevAttribute = attrib;
        }
        // end of the tag
        else if ( *p == '>' ) {
            ++p;
            break;
        }
        // end of the tag
        else if ( *p == '/' && *(p+1) == '>' ) {
            _closingType = CLOSED;
            return p+2;	// done; sealed element.
        }
        else {
            _document->SetError( XML_ERROR_PARSING_ELEMENT, start, p );
            return 0;
        }
    }
    return p;
}

void XmlElement::DeleteAttribute( XmlAttribute* attribute )
{
    if ( attribute == 0 ) {
        return;
    }
    MemPool* pool = attribute->_memPool;
    attribute->~XmlAttribute();
    pool->Free( attribute );
}

//
//	<ele></ele>
//	<ele>foo<b>bar</b></ele>
//
char* XmlElement::ParseDeep( char* p, StrPair* strPair )
{
    // Read the element name.
    p = XmlUtil::SkipWhiteSpace( p );

    // The closing element is the </element> form. It is
    // parsed just like a regular element then deleted from
    // the DOM.
    if ( *p == '/' ) {
        _closingType = CLOSING;
        ++p;
    }

    p = _value.ParseName( p );
    if ( _value.Empty() ) {
        return 0;
    }

    p = ParseAttributes( p );
    if ( !p || !*p || _closingType ) {
        return p;
    }

    p = XmlNode::ParseDeep( p, strPair );
    return p;
}



XmlNode* XmlElement::ShallowClone( XmlDocument* doc ) const
{
    if ( !doc ) {
        doc = _document;
    }
    XmlElement* element = doc->NewElement( Value() );					// fixme: this will always allocate memory. Intern?
    for( const XmlAttribute* a=FirstAttribute(); a; a=a->Next() ) {
        element->SetAttribute( a->Name(), a->Value() );					// fixme: this will always allocate memory. Intern?
    }
    return element;
}


bool XmlElement::ShallowEqual( const XmlNode* compare ) const
{
    TIXMLASSERT( compare );
    const XmlElement* other = compare->ToElement();
    if ( other && XmlUtil::StringEqual( other->Name(), Name() )) {

        const XmlAttribute* a=FirstAttribute();
        const XmlAttribute* b=other->FirstAttribute();

        while ( a && b ) {
            if ( !XmlUtil::StringEqual( a->Value(), b->Value() ) ) {
                return false;
            }
            a = a->Next();
            b = b->Next();
        }
        if ( a || b ) {
            // different count
            return false;
        }
        return true;
    }
    return false;
}


bool XmlElement::Accept( XmlVisitor* visitor ) const
{
    TIXMLASSERT( visitor );
    if ( visitor->VisitEnter( *this, _rootAttribute ) ) {
        for ( const XmlNode* node=FirstChild(); node; node=node->NextSibling() ) {
            if ( !node->Accept( visitor ) ) {
                break;
            }
        }
    }
    return visitor->VisitExit( *this );
}


// --------- XmlDocument ----------- //

// Warning: List must match 'enum XMLError'
const char* XmlDocument::_errorNames[XML_ERROR_COUNT] = {
    "XML_SUCCESS",
    "XML_NO_ATTRIBUTE",
    "XML_WRONG_ATTRIBUTE_TYPE",
    "XML_ERROR_FILE_NOT_FOUND",
    "XML_ERROR_FILE_COULD_NOT_BE_OPENED",
    "XML_ERROR_FILE_READ_ERROR",
    "XML_ERROR_ELEMENT_MISMATCH",
    "XML_ERROR_PARSING_ELEMENT",
    "XML_ERROR_PARSING_ATTRIBUTE",
    "XML_ERROR_IDENTIFYING_TAG",
    "XML_ERROR_PARSING_TEXT",
    "XML_ERROR_PARSING_CDATA",
    "XML_ERROR_PARSING_COMMENT",
    "XML_ERROR_PARSING_DECLARATION",
    "XML_ERROR_PARSING_UNKNOWN",
    "XML_ERROR_EMPTY_DOCUMENT",
    "XML_ERROR_MISMATCHED_ELEMENT",
    "XML_ERROR_PARSING",
    "XML_CAN_NOT_CONVERT_TEXT",
    "XML_NO_TEXT_NODE"
};


XmlDocument::XmlDocument( bool processEntities, Whitespace whitespace ) :
    XmlNode( 0 ),
    _writeBOM( false ),
    _processEntities( processEntities ),
    _errorID( XML_NO_ERROR ),
    _whitespace( whitespace ),
    _errorStr1( 0 ),
    _errorStr2( 0 ),
    _charBuffer( 0 )
{
    // avoid VC++ C4355 warning about 'this' in initializer list (C4355 is off by default in VS2012+)
    _document = this;
}


XmlDocument::~XmlDocument()
{
    Clear();
}


void XmlDocument::Clear()
{
    DeleteChildren();

#ifdef DEBUG
    const bool hadError = Error();
#endif
    _errorID = XML_NO_ERROR;
    _errorStr1 = 0;
    _errorStr2 = 0;

    delete [] _charBuffer;
    _charBuffer = 0;

#if 0
    _textPool.Trace( "text" );
    _elementPool.Trace( "element" );
    _commentPool.Trace( "comment" );
    _attributePool.Trace( "attribute" );
#endif
    
#ifdef DEBUG
    if ( !hadError ) {
        TIXMLASSERT( _elementPool.CurrentAllocs()   == _elementPool.Untracked() );
        TIXMLASSERT( _attributePool.CurrentAllocs() == _attributePool.Untracked() );
        TIXMLASSERT( _textPool.CurrentAllocs()      == _textPool.Untracked() );
        TIXMLASSERT( _commentPool.CurrentAllocs()   == _commentPool.Untracked() );
    }
#endif
}


XmlElement* XmlDocument::NewElement( const char* name )
{
    TIXMLASSERT( sizeof( XmlElement ) == _elementPool.ItemSize() );
    XmlElement* ele = new (_elementPool.Alloc()) XmlElement( this );
    ele->_memPool = &_elementPool;
    ele->SetName( name );
    return ele;
}


XmlComment* XmlDocument::NewComment( const char* str )
{
    TIXMLASSERT( sizeof( XmlComment ) == _commentPool.ItemSize() );
    XmlComment* comment = new (_commentPool.Alloc()) XmlComment( this );
    comment->_memPool = &_commentPool;
    comment->SetValue( str );
    return comment;
}


XmlText* XmlDocument::NewText( const char* str )
{
    TIXMLASSERT( sizeof( XmlText ) == _textPool.ItemSize() );
    XmlText* text = new (_textPool.Alloc()) XmlText( this );
    text->_memPool = &_textPool;
    text->SetValue( str );
    return text;
}


XmlDeclaration* XmlDocument::NewDeclaration( const char* str )
{
    TIXMLASSERT( sizeof( XmlDeclaration ) == _commentPool.ItemSize() );
    XmlDeclaration* dec = new (_commentPool.Alloc()) XmlDeclaration( this );
    dec->_memPool = &_commentPool;
    dec->SetValue( str ? str : "xml version=\"1.0\" encoding=\"UTF-8\"" );
    return dec;
}


XmlUnknown* XmlDocument::NewUnknown( const char* str )
{
    TIXMLASSERT( sizeof( XmlUnknown ) == _commentPool.ItemSize() );
    XmlUnknown* unk = new (_commentPool.Alloc()) XmlUnknown( this );
    unk->_memPool = &_commentPool;
    unk->SetValue( str );
    return unk;
}

static FILE* callfopen( const char* filepath, const char* mode )
{
    TIXMLASSERT( filepath );
    TIXMLASSERT( mode );
#if defined(_MSC_VER) && (_MSC_VER >= 1400 ) && (!defined WINCE)
    FILE* fp = 0;
    errno_t err = fopen_s( &fp, filepath, mode );
    if ( err ) {
        return 0;
    }
#else
    FILE* fp = fopen( filepath, mode );
#endif
    return fp;
}
    
void XmlDocument::DeleteNode( XmlNode* node )	{
    TIXMLASSERT( node );
    TIXMLASSERT(node->_document == this );
    if (node->_parent) {
        node->_parent->DeleteChild( node );
    }
    else {
        // Isn't in the tree.
        // Use the parent delete.
        // Also, we need to mark it tracked: we 'know'
        // it was never used.
        node->_memPool->SetTracked();
        // Call the static XmlNode version:
        XmlNode::DeleteNode(node);
    }
}


XMLError XmlDocument::LoadFile( const char* filename )
{
    Clear();
    FILE* fp = callfopen( filename, "rb" );
    if ( !fp ) {
        SetError( XML_ERROR_FILE_NOT_FOUND, filename, 0 );
        return _errorID;
    }
    LoadFile( fp );
    fclose( fp );
    return _errorID;
}

// This is likely overengineered template art to have a check that unsigned long value incremented
// by one still fits into size_t. If size_t type is larger than unsigned long type
// (x86_64-w64-mingw32 target) then the check is redundant and gcc and clang emit
// -Wtype-limits warning. This piece makes the compiler select code with a check when a check
// is useful and code with no check when a check is redundant depending on how size_t and unsigned long
// types sizes relate to each other.
template
<bool = (sizeof(unsigned long) >= sizeof(size_t))>
struct LongFitsIntoSizeTMinusOne {
    static bool Fits( unsigned long value )
    {
        return value < (size_t)-1;
    }
};

template <>
bool LongFitsIntoSizeTMinusOne<false>::Fits( unsigned long /*value*/ )
{
    return true;
}

XMLError XmlDocument::LoadFile( FILE* fp )
{
    Clear();

    fseek( fp, 0, SEEK_SET );
    if ( fgetc( fp ) == EOF && ferror( fp ) != 0 ) {
        SetError( XML_ERROR_FILE_READ_ERROR, 0, 0 );
        return _errorID;
    }

    fseek( fp, 0, SEEK_END );
    const long filelength = ftell( fp );
    fseek( fp, 0, SEEK_SET );
    if ( filelength == -1L ) {
        SetError( XML_ERROR_FILE_READ_ERROR, 0, 0 );
        return _errorID;
    }
    TIXMLASSERT( filelength >= 0 );

    if ( !LongFitsIntoSizeTMinusOne<>::Fits( filelength ) ) {
        // Cannot handle files which won't fit in buffer together with null terminator
        SetError( XML_ERROR_FILE_READ_ERROR, 0, 0 );
        return _errorID;
    }

    if ( filelength == 0 ) {
        SetError( XML_ERROR_EMPTY_DOCUMENT, 0, 0 );
        return _errorID;
    }

    const size_t size = filelength;
    TIXMLASSERT( _charBuffer == 0 );
    _charBuffer = new char[size+1];
    size_t read = fread( _charBuffer, 1, size, fp );
    if ( read != size ) {
        SetError( XML_ERROR_FILE_READ_ERROR, 0, 0 );
        return _errorID;
    }

    _charBuffer[size] = 0;

    Parse();
    return _errorID;
}


XMLError XmlDocument::SaveFile( const char* filename, bool compact )
{
    FILE* fp = callfopen( filename, "w" );
    if ( !fp ) {
        SetError( XML_ERROR_FILE_COULD_NOT_BE_OPENED, filename, 0 );
        return _errorID;
    }
    SaveFile(fp, compact);
    fclose( fp );
    return _errorID;
}


XMLError XmlDocument::SaveFile( FILE* fp, bool compact )
{
    // Clear any error from the last save, otherwise it will get reported
    // for *this* call.
    SetError( XML_NO_ERROR, 0, 0 );
    XmlPrinter stream( fp, compact );
    Print( &stream );
    return _errorID;
}


XMLError XmlDocument::Parse( const char* p, size_t len )
{
    Clear();

    if ( len == 0 || !p || !*p ) {
        SetError( XML_ERROR_EMPTY_DOCUMENT, 0, 0 );
        return _errorID;
    }
    if ( len == (size_t)(-1) ) {
        len = strlen( p );
    }
    TIXMLASSERT( _charBuffer == 0 );
    _charBuffer = new char[ len+1 ];
    memcpy( _charBuffer, p, len );
    _charBuffer[len] = 0;

    Parse();
    if ( Error() ) {
        // clean up now essentially dangling memory.
        // and the parse fail can put objects in the
        // pools that are dead and inaccessible.
        DeleteChildren();
        _elementPool.Clear();
        _attributePool.Clear();
        _textPool.Clear();
        _commentPool.Clear();
    }
    return _errorID;
}


void XmlDocument::Print( XmlPrinter* streamer ) const
{
    if ( streamer ) {
        Accept( streamer );
    }
    else {
        XmlPrinter stdoutStreamer( stdout );
        Accept( &stdoutStreamer );
    }
}


void XmlDocument::SetError( XMLError error, const char* str1, const char* str2 )
{
    TIXMLASSERT( error >= 0 && error < XML_ERROR_COUNT );
    _errorID = error;
    _errorStr1 = str1;
    _errorStr2 = str2;
}

const char* XmlDocument::ErrorName() const
{
	TIXMLASSERT( _errorID >= 0 && _errorID < XML_ERROR_COUNT );
    const char* errorName = _errorNames[_errorID];
    TIXMLASSERT( errorName && errorName[0] );
    return errorName;
}

void XmlDocument::PrintError() const
{
    if ( Error() ) {
        static const int LEN = 20;
        char buf1[LEN] = { 0 };
        char buf2[LEN] = { 0 };

        if ( _errorStr1 ) {
            TIXML_SNPRINTF( buf1, LEN, "%s", _errorStr1 );
        }
        if ( _errorStr2 ) {
            TIXML_SNPRINTF( buf2, LEN, "%s", _errorStr2 );
        }

        // Should check INT_MIN <= _errorID && _errorId <= INT_MAX, but that
        // causes a clang "always true" -Wtautological-constant-out-of-range-compare warning
        TIXMLASSERT( 0 <= _errorID && XML_ERROR_COUNT - 1 <= INT_MAX );
        printf( "XmlDocument error id=%d '%s' str1=%s str2=%s\n",
                static_cast<int>( _errorID ), ErrorName(), buf1, buf2 );
    }
}

void XmlDocument::Parse()
{
    TIXMLASSERT( NoChildren() ); // Clear() must have been called previously
    TIXMLASSERT( _charBuffer );
    char* p = _charBuffer;
    p = XmlUtil::SkipWhiteSpace( p );
    p = const_cast<char*>( XmlUtil::ReadBOM( p, &_writeBOM ) );
    if ( !*p ) {
        SetError( XML_ERROR_EMPTY_DOCUMENT, 0, 0 );
        return;
    }
    ParseDeep(p, 0 );
}

XmlPrinter::XmlPrinter( FILE* file, bool compact, int depth ) :
    _elementJustOpened( false ),
    _firstElement( true ),
    _fp( file ),
    _depth( depth ),
    _textDepth( -1 ),
    _processEntities( true ),
    _compactMode( compact )
{
    for( int i=0; i<ENTITY_RANGE; ++i ) {
        _entityFlag[i] = false;
        _restrictedEntityFlag[i] = false;
    }
    for( int i=0; i<NUM_ENTITIES; ++i ) {
        const char entityValue = entities[i].value;
        TIXMLASSERT( 0 <= entityValue && entityValue < ENTITY_RANGE );
        _entityFlag[ (unsigned char)entityValue ] = true;
    }
    _restrictedEntityFlag[(unsigned char)'&'] = true;
    _restrictedEntityFlag[(unsigned char)'<'] = true;
    _restrictedEntityFlag[(unsigned char)'>'] = true;	// not required, but consistency is nice
    _buffer.Push( 0 );
}


void XmlPrinter::Print( const char* format, ... )
{
    va_list     va;
    va_start( va, format );

    if ( _fp ) {
        vfprintf( _fp, format, va );
    }
    else {
        const int len = TIXML_VSCPRINTF( format, va );
        // Close out and re-start the va-args
        va_end( va );
        TIXMLASSERT( len >= 0 );
        va_start( va, format );
        TIXMLASSERT( _buffer.Size() > 0 && _buffer[_buffer.Size() - 1] == 0 );
        char* p = _buffer.PushArr( len ) - 1;	// back up over the null terminator.
		TIXML_VSNPRINTF( p, len+1, format, va );
    }
    va_end( va );
}


void XmlPrinter::PrintSpace( int depth )
{
    for( int i=0; i<depth; ++i ) {
        Print( "    " );
    }
}


void XmlPrinter::PrintString( const char* p, bool restricted )
{
    // Look for runs of bytes between entities to print.
    const char* q = p;

    if ( _processEntities ) {
        const bool* flag = restricted ? _restrictedEntityFlag : _entityFlag;
        while ( *q ) {
            TIXMLASSERT( p <= q );
            // Remember, char is sometimes signed. (How many times has that bitten me?)
            if ( *q > 0 && *q < ENTITY_RANGE ) {
                // Check for entities. If one is found, flush
                // the stream up until the entity, write the
                // entity, and keep looking.
                if ( flag[(unsigned char)(*q)] ) {
                    while ( p < q ) {
                        const size_t delta = q - p;
                        // %.*s accepts type int as "precision"
                        const int toPrint = ( INT_MAX < delta ) ? INT_MAX : (int)delta;
                        Print( "%.*s", toPrint, p );
                        p += toPrint;
                    }
                    bool entityPatternPrinted = false;
                    for( int i=0; i<NUM_ENTITIES; ++i ) {
                        if ( entities[i].value == *q ) {
                            Print( "&%s;", entities[i].pattern );
                            entityPatternPrinted = true;
                            break;
                        }
                    }
                    if ( !entityPatternPrinted ) {
                        // TIXMLASSERT( entityPatternPrinted ) causes gcc -Wunused-but-set-variable in release
                        TIXMLASSERT( false );
                    }
                    ++p;
                }
            }
            ++q;
            TIXMLASSERT( p <= q );
        }
    }
    // Flush the remaining string. This will be the entire
    // string if an entity wasn't found.
    TIXMLASSERT( p <= q );
    if ( !_processEntities || ( p < q ) ) {
        Print( "%s", p );
    }
}


void XmlPrinter::PushHeader( bool writeBOM, bool writeDec )
{
    if ( writeBOM ) {
        static const unsigned char bom[] = { TIXML_UTF_LEAD_0, TIXML_UTF_LEAD_1, TIXML_UTF_LEAD_2, 0 };
        Print( "%s", bom );
    }
    if ( writeDec ) {
        PushDeclaration( "xml version=\"1.0\"" );
    }
}


void XmlPrinter::OpenElement( const char* name, bool compactMode )
{
    SealElementIfJustOpened();
    _stack.Push( name );

    if ( _textDepth < 0 && !_firstElement && !compactMode ) {
        Print( "\n" );
    }
    if ( !compactMode ) {
        PrintSpace( _depth );
    }

    Print( "<%s", name );
    _elementJustOpened = true;
    _firstElement = false;
    ++_depth;
}


void XmlPrinter::PushAttribute( const char* name, const char* value )
{
    TIXMLASSERT( _elementJustOpened );
    Print( " %s=\"", name );
    PrintString( value, false );
    Print( "\"" );
}


void XmlPrinter::PushAttribute( const char* name, int v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    PushAttribute( name, buf );
}


void XmlPrinter::PushAttribute( const char* name, unsigned v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    PushAttribute( name, buf );
}


void XmlPrinter::PushAttribute( const char* name, bool v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    PushAttribute( name, buf );
}


void XmlPrinter::PushAttribute( const char* name, double v )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( v, buf, BUF_SIZE );
    PushAttribute( name, buf );
}


void XmlPrinter::CloseElement( bool compactMode )
{
    --_depth;
    const char* name = _stack.Pop();

    if ( _elementJustOpened ) {
        Print( "/>" );
    }
    else {
        if ( _textDepth < 0 && !compactMode) {
            Print( "\n" );
            PrintSpace( _depth );
        }
        Print( "</%s>", name );
    }

    if ( _textDepth == _depth ) {
        _textDepth = -1;
    }
    if ( _depth == 0 && !compactMode) {
        Print( "\n" );
    }
    _elementJustOpened = false;
}


void XmlPrinter::SealElementIfJustOpened()
{
    if ( !_elementJustOpened ) {
        return;
    }
    _elementJustOpened = false;
    Print( ">" );
}


void XmlPrinter::PushText( const char* text, bool cdata )
{
    _textDepth = _depth-1;

    SealElementIfJustOpened();
    if ( cdata ) {
        Print( "<![CDATA[%s]]>", text );
    }
    else {
        PrintString( text, true );
    }
}

void XmlPrinter::PushText( int value )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( value, buf, BUF_SIZE );
    PushText( buf, false );
}


void XmlPrinter::PushText( unsigned value )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( value, buf, BUF_SIZE );
    PushText( buf, false );
}


void XmlPrinter::PushText( bool value )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( value, buf, BUF_SIZE );
    PushText( buf, false );
}


void XmlPrinter::PushText( float value )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( value, buf, BUF_SIZE );
    PushText( buf, false );
}


void XmlPrinter::PushText( double value )
{
    char buf[BUF_SIZE];
    XmlUtil::ToStr( value, buf, BUF_SIZE );
    PushText( buf, false );
}


void XmlPrinter::PushComment( const char* comment )
{
    SealElementIfJustOpened();
    if ( _textDepth < 0 && !_firstElement && !_compactMode) {
        Print( "\n" );
        PrintSpace( _depth );
    }
    _firstElement = false;
    Print( "<!--%s-->", comment );
}


void XmlPrinter::PushDeclaration( const char* value )
{
    SealElementIfJustOpened();
    if ( _textDepth < 0 && !_firstElement && !_compactMode) {
        Print( "\n" );
        PrintSpace( _depth );
    }
    _firstElement = false;
    Print( "<?%s?>", value );
}


void XmlPrinter::PushUnknown( const char* value )
{
    SealElementIfJustOpened();
    if ( _textDepth < 0 && !_firstElement && !_compactMode) {
        Print( "\n" );
        PrintSpace( _depth );
    }
    _firstElement = false;
    Print( "<!%s>", value );
}


bool XmlPrinter::VisitEnter( const XmlDocument& doc )
{
    _processEntities = doc.ProcessEntities();
    if ( doc.HasBOM() ) {
        PushHeader( true, false );
    }
    return true;
}


bool XmlPrinter::VisitEnter( const XmlElement& element, const XmlAttribute* attribute )
{
    const XmlElement* parentElem = 0;
    if ( element.Parent() ) {
        parentElem = element.Parent()->ToElement();
    }
    const bool compactMode = parentElem ? CompactMode( *parentElem ) : _compactMode;
    OpenElement( element.Name(), compactMode );
    while ( attribute ) {
        PushAttribute( attribute->Name(), attribute->Value() );
        attribute = attribute->Next();
    }
    return true;
}


bool XmlPrinter::VisitExit( const XmlElement& element )
{
    CloseElement( CompactMode(element) );
    return true;
}


bool XmlPrinter::Visit( const XmlText& text )
{
    PushText( text.Value(), text.CData() );
    return true;
}


bool XmlPrinter::Visit( const XmlComment& comment )
{
    PushComment( comment.Value() );
    return true;
}

bool XmlPrinter::Visit( const XmlDeclaration& declaration )
{
    PushDeclaration( declaration.Value() );
    return true;
}


bool XmlPrinter::Visit( const XmlUnknown& unknown )
{
    PushUnknown( unknown.Value() );
    return true;
}

}   // namespace tinyxml2

