# Value Object Catalog

## Overview

Value objects are immutable domain concepts identified by their attributes rather than identity. This catalog documents all value objects in the Japanese company model.

## Characteristics of Value Objects

1. **Immutability**: Once created, cannot be modified
2. **Value Equality**: Two value objects are equal if all attributes are equal
3. **Self-Validation**: Constructor enforces all invariants
4. **No Identity**: No unique identifier
5. **Replaceability**: Can be replaced with another instance with same values

## Core Value Objects

### Money

**Purpose**: Represent monetary amounts with precision and currency.

```haskell
data Money = Money
  { amount :: Scientific      -- Arbitrary precision
  , currency :: Currency
  }
  deriving (Eq, Ord)

data Currency
  = JPY  -- Japanese Yen
  | USD  -- US Dollar
  | EUR  -- Euro
  | GBP  -- British Pound
  deriving (Eq, Ord, Enum)

-- Smart constructor
mkMoney :: Scientific -> Currency -> Maybe Money
mkMoney amt curr
  | amt >= 0 = Just (Money amt curr)
  | otherwise = Nothing

-- Japanese Yen (no fractional yen)
newtype JPY = JPY Integer
  deriving (Eq, Ord, Num)

-- Operations
add :: Money -> Money -> Maybe Money
add (Money a1 c1) (Money a2 c2)
  | c1 == c2 = Just (Money (a1 + a2) c1)
  | otherwise = Nothing  -- Cannot add different currencies

multiply :: Money -> Scientific -> Money
multiply (Money amt curr) factor =
  Money (amt * factor) curr

-- Display
instance Show Money where
  show (Money amt JPY) = "¥" <> formatNumber amt
  show (Money amt USD) = "$" <> formatNumber amt

-- Round to currency precision
roundToJPY :: Money -> Money
roundToJPY (Money amt JPY) =
  Money (fromInteger (round amt)) JPY
```

### PersonName (Japanese)

**Purpose**: Represent Japanese personal names with proper structure.

```haskell
data PersonName = PersonName
  { familyName :: FamilyName
  , givenName :: GivenName
  , middleName :: Maybe MiddleName
  }
  deriving (Eq)

newtype FamilyName = FamilyName Text
newtype GivenName = GivenName Text
newtype MiddleName = MiddleName Text

-- Kana representation (phonetic)
data PersonNameKana = PersonNameKana
  { familyNameKana :: FamilyNameKana
  , givenNameKana :: GivenNameKana
  }
  deriving (Eq)

newtype FamilyNameKana = FamilyNameKana Text  -- Katakana only
newtype GivenNameKana = GivenNameKana Text    -- Katakana only

-- Smart constructor validates katakana
mkPersonNameKana :: Text -> Text -> Maybe PersonNameKana
mkPersonNameKana family given
  | isKatakana family && isKatakana given =
      Just $ PersonNameKana
        (FamilyNameKana family)
        (GivenNameKana given)
  | otherwise = Nothing

-- Display formats
fullName :: PersonName -> Text
fullName (PersonName family given _) =
  unFamilyName family <> " " <> unGivenName given

fullNameJapanese :: PersonName -> Text  -- Family name first
fullNameJapanese (PersonName family given _) =
  unFamilyName family <> unGivenName given

fullNameKana :: PersonNameKana -> Text
fullNameKana (PersonNameKana family given) =
  unFamilyNameKana family <> " " <> unGivenNameKana given
```

### Address (Japanese)

**Purpose**: Represent Japanese addresses with proper structure.

```haskell
data Address = Address
  { postalCode :: PostalCode
  , prefecture :: Prefecture
  , city :: City
  , ward :: Maybe Ward
  , street :: StreetAddress
  , building :: Maybe BuildingInfo
  , roomNumber :: Maybe RoomNumber
  }
  deriving (Eq)

-- Postal code: NNN-NNNN format
newtype PostalCode = PostalCode Text
  deriving (Eq)

mkPostalCode :: Text -> Maybe PostalCode
mkPostalCode code
  | Text.length code == 8 &&
    Text.index code 3 == '-' &&
    Text.all isDigit (Text.filter (/= '-') code) =
      Just (PostalCode code)
  | otherwise = Nothing

-- 47 prefectures
data Prefecture
  = Hokkaido    -- 北海道
  | Aomori      -- 青森県
  | Iwate       -- 岩手県
  | Miyagi      -- 宮城県
  | Akita       -- 秋田県
  | Yamagata    -- 山形県
  | Fukushima   -- 福島県
  | Ibaraki     -- 茨城県
  | Tochigi     -- 栃木県
  | Gunma       -- 群馬県
  | Saitama     -- 埼玉県
  | Chiba       -- 千葉県
  | Tokyo       -- 東京都
  | Kanagawa    -- 神奈川県
  | Niigata     -- 新潟県
  | Toyama      -- 富山県
  | Ishikawa    -- 石川県
  | Fukui       -- 福井県
  | Yamanashi   -- 山梨県
  | Nagano      -- 長野県
  | Gifu        -- 岐阜県
  | Shizuoka    -- 静岡県
  | Aichi       -- 愛知県
  | Mie         -- 三重県
  | Shiga       -- 滋賀県
  | Kyoto       -- 京都府
  | Osaka       -- 大阪府
  | Hyogo       -- 兵庫県
  | Nara        -- 奈良県
  | Wakayama    -- 和歌山県
  | Tottori     -- 鳥取県
  | Shimane     -- 島根県
  | Okayama     -- 岡山県
  | Hiroshima   -- 広島県
  | Yamaguchi   -- 山口県
  | Tokushima   -- 徳島県
  | Kagawa      -- 香川県
  | Ehime       -- 愛媛県
  | Kochi       -- 高知県
  | Fukuoka     -- 福岡県
  | Saga        -- 佐賀県
  | Nagasaki    -- 長崎県
  | Kumamoto    -- 熊本県
  | Oita        -- 大分県
  | Miyazaki    -- 宮崎県
  | Kagoshima   -- 鹿児島県
  | Okinawa     -- 沖縄県
  deriving (Eq, Ord, Enum, Bounded)

-- Prefecture suffix
prefectureSuffix :: Prefecture -> Text
prefectureSuffix Hokkaido = ""      -- No suffix
prefectureSuffix Tokyo = "都"       -- Metropolis
prefectureSuffix Osaka = "府"       -- Urban prefecture
prefectureSuffix Kyoto = "府"       -- Urban prefecture
prefectureSuffix _ = "県"           -- Prefecture

newtype City = City Text
newtype Ward = Ward Text            -- 区 (ku)
newtype StreetAddress = StreetAddress Text

data BuildingInfo = BuildingInfo
  { buildingName :: BuildingName
  , buildingNumber :: Maybe BuildingNumber
  }

-- Format address Japanese style
formatAddress :: Address -> Text
formatAddress addr =
  Text.intercalate " "
    [ "〒" <> unPostalCode (postalCode addr)
    , prefectureName (prefecture addr) <> prefectureSuffix (prefecture addr)
    , unCity (city addr)
    , maybe "" unWard (ward addr)
    , unStreetAddress (street addr)
    , maybe "" formatBuilding (building addr)
    , maybe "" unRoomNumber (roomNumber addr)
    ]
```

### CorporateNumber

**Purpose**: 13-digit corporate identifier issued by Japanese government.

```haskell
newtype CorporateNumber = CorporateNumber Text
  deriving (Eq, Ord)

-- Smart constructor with validation
mkCorporateNumber :: Text -> Maybe CorporateNumber
mkCorporateNumber num
  | Text.length num == 13 &&
    Text.all isDigit num &&
    validCheckDigit num =
      Just (CorporateNumber num)
  | otherwise = Nothing

-- Check digit validation (Luhn algorithm)
validCheckDigit :: Text -> Bool
validCheckDigit num =
  let digits = map digitToInt (Text.unpack num)
      checkDigit = head digits
      payload = tail digits
      calculated = calculateCheckDigit payload
  in checkDigit == calculated

calculateCheckDigit :: [Int] -> Int
calculateCheckDigit digits =
  let weighted = zipWith (*) (cycle [2, 1]) digits
      summed = sum $ map (\n -> n `div` 10 + n `mod` 10) weighted
  in (10 - (summed `mod` 10)) `mod` 10

-- Format for display: NNNN-NNNN-NNNNN
formatCorporateNumber :: CorporateNumber -> Text
formatCorporateNumber (CorporateNumber num) =
  let (part1, rest) = Text.splitAt 4 num
      (part2, part3) = Text.splitAt 4 rest
  in part1 <> "-" <> part2 <> "-" <> part3
```

### EmployeeNumber

**Purpose**: Company-specific employee identifier.

```haskell
newtype EmployeeNumber = EmployeeNumber Text
  deriving (Eq, Ord)

-- Format: YYYY-NNNN (year + sequence)
data EmployeeNumberFormat = EmployeeNumberFormat
  { year :: Year
  , sequence :: SequenceNumber
  }

mkEmployeeNumber :: Year -> SequenceNumber -> EmployeeNumber
mkEmployeeNumber year seq =
  EmployeeNumber $
    Text.pack (show year) <> "-" <>
    Text.pack (printf "%04d" seq)

parseEmployeeNumber :: EmployeeNumber -> Maybe EmployeeNumberFormat
parseEmployeeNumber (EmployeeNumber num) =
  case Text.splitOn "-" num of
    [yearText, seqText] -> do
      year <- readMaybe (Text.unpack yearText)
      seq <- readMaybe (Text.unpack seqText)
      pure $ EmployeeNumberFormat year seq
    _ -> Nothing
```

### TaxCategory

**Purpose**: Consumption tax classification.

```haskell
data TaxCategory
  = StandardRate                  -- 標準税率 10%
  | ReducedRate                   -- 軽減税率 8%
  | TaxExempt                     -- 非課税
  | TaxFree                       -- 免税 (exports)
  | OutOfScope                    -- 不課税
  deriving (Eq, Ord)

data TaxRate = TaxRate
  { nationalRate :: Percentage    -- 7.8% of 10%
  , localRate :: Percentage       -- 2.2% of 10%
  , effectiveFrom :: Date
  , effectiveTo :: Maybe Date
  }

currentStandardRate :: TaxRate
currentStandardRate = TaxRate
  { nationalRate = 7.8
  , localRate = 2.2
  , effectiveFrom = fromGregorian 2019 10 1
  , effectiveTo = Nothing
  }

currentReducedRate :: TaxRate
currentReducedRate = TaxRate
  { nationalRate = 6.24
  , localRate = 1.76
  , effectiveFrom = fromGregorian 2019 10 1
  , effectiveTo = Nothing
  }

-- Calculate tax amount
calculateTax :: TaxCategory -> Money -> Maybe Money
calculateTax StandardRate amount =
  Just $ multiply amount 0.10
calculateTax ReducedRate amount =
  Just $ multiply amount 0.08
calculateTax TaxExempt _ = Just (Money 0 JPY)
calculateTax TaxFree _ = Just (Money 0 JPY)
calculateTax OutOfScope _ = Nothing
```

### FiscalYearEnd

**Purpose**: Company's fiscal year end date.

```haskell
data FiscalYearEnd = FiscalYearEnd
  { month :: Month
  , day :: DayOfMonth
  }
  deriving (Eq, Ord)

data Month
  = January | February | March | April | May | June
  | July | August | September | October | November | December
  deriving (Eq, Ord, Enum, Bounded)

newtype DayOfMonth = DayOfMonth Int
  deriving (Eq, Ord)

mkFiscalYearEnd :: Month -> DayOfMonth -> Maybe FiscalYearEnd
mkFiscalYearEnd month (DayOfMonth day)
  | validDay month day = Just (FiscalYearEnd month (DayOfMonth day))
  | otherwise = Nothing
  where
    validDay March 31 = True
    validDay December 31 = True
    validDay September 30 = True
    validDay June 30 = True
    validDay m d = d <= daysInMonth m

-- Common fiscal year ends in Japan
commonFiscalYearEnds :: [FiscalYearEnd]
commonFiscalYearEnds =
  [ FiscalYearEnd March (DayOfMonth 31)      -- Most common
  , FiscalYearEnd December (DayOfMonth 31)   -- Calendar year
  , FiscalYearEnd September (DayOfMonth 30)  -- Also common
  ]

-- Calculate fiscal year start from end
fiscalYearStart :: FiscalYearEnd -> Int -> Day
fiscalYearStart (FiscalYearEnd month (DayOfMonth day)) year =
  let endDate = fromGregorian year (fromEnum month + 1) day
  in addDays 1 (addGregorianYearsClip (-1) endDate)
```

### PhoneNumber

**Purpose**: Japanese phone number with proper formatting.

```haskell
data PhoneNumber
  = LandLine
      { areaCode :: AreaCode
      , exchange :: Exchange
      , number :: Number
      }
  | MobilePhone
      { carrierCode :: CarrierCode
      , number :: Number
      }
  | FreePhone
      { number :: Number  -- 0120 or 0800
      }
  deriving (Eq)

newtype AreaCode = AreaCode Text      -- 03, 06, 011, etc.
newtype Exchange = Exchange Text      -- NNNN
newtype CarrierCode = CarrierCode Text -- 070, 080, 090
newtype Number = Number Text

-- Smart constructor
mkLandLine :: Text -> Text -> Text -> Maybe PhoneNumber
mkLandLine area ex num
  | validAreaCode area && validExchange ex && validNumber num =
      Just $ LandLine (AreaCode area) (Exchange ex) (Number num)
  | otherwise = Nothing

mkMobilePhone :: Text -> Text -> Maybe PhoneNumber
mkMobilePhone carrier num
  | validCarrierCode carrier && validNumber num =
      Just $ MobilePhone (CarrierCode carrier) (Number num)
  | otherwise = Nothing

-- Format for display
formatPhoneNumber :: PhoneNumber -> Text
formatPhoneNumber (LandLine (AreaCode area) (Exchange ex) (Number num)) =
  area <> "-" <> ex <> "-" <> num
formatPhoneNumber (MobilePhone (CarrierCode carrier) (Number num)) =
  carrier <> "-" <> Text.take 4 num <> "-" <> Text.drop 4 num
formatPhoneNumber (FreePhone (Number num)) =
  "0120-" <> Text.take 3 num <> "-" <> Text.drop 3 num
```

### Email

**Purpose**: Email address with validation.

```haskell
newtype Email = Email Text
  deriving (Eq, Ord)

mkEmail :: Text -> Maybe Email
mkEmail addr
  | validEmail addr = Just (Email addr)
  | otherwise = Nothing

validEmail :: Text -> Bool
validEmail addr =
  let parts = Text.splitOn "@" addr
  in length parts == 2 &&
     all (not . Text.null) parts &&
     Text.any (== '.') (parts !! 1)

-- Extract domain
emailDomain :: Email -> Text
emailDomain (Email addr) =
  Text.drop 1 $ Text.dropWhile (/= '@') addr
```

### DateRange

**Purpose**: Inclusive date range.

```haskell
data DateRange = DateRange
  { startDate :: Date
  , endDate :: Date
  }
  deriving (Eq)

mkDateRange :: Date -> Date -> Maybe DateRange
mkDateRange start end
  | start <= end = Just (DateRange start end)
  | otherwise = Nothing

-- Check if date is in range
inRange :: Date -> DateRange -> Bool
inRange date (DateRange start end) =
  date >= start && date <= end

-- Calculate duration
duration :: DateRange -> Days
duration (DateRange start end) =
  diffDays end start + 1

-- Check for overlap
overlaps :: DateRange -> DateRange -> Bool
overlaps (DateRange s1 e1) (DateRange s2 e2) =
  not (e1 < s2 || e2 < s1)
```

### Percentage

**Purpose**: Percentage value with proper precision.

```haskell
newtype Percentage = Percentage Scientific
  deriving (Eq, Ord, Num)

mkPercentage :: Scientific -> Maybe Percentage
mkPercentage p
  | p >= 0 && p <= 100 = Just (Percentage p)
  | otherwise = Nothing

toDecimal :: Percentage -> Scientific
toDecimal (Percentage p) = p / 100

fromDecimal :: Scientific -> Maybe Percentage
fromDecimal d
  | d >= 0 && d <= 1 = Just (Percentage (d * 100))
  | otherwise = Nothing

-- Format for display
instance Show Percentage where
  show (Percentage p) = formatScientific Fixed (Just 2) p <> "%"
```

## Value Object Best Practices

### 1. Always Use Smart Constructors

```haskell
-- ❌ BAD: Public constructor
data PostalCode = PostalCode Text

-- ✅ GOOD: Smart constructor
newtype PostalCode = PostalCode Text

mkPostalCode :: Text -> Maybe PostalCode
mkPostalCode code
  | valid code = Just (PostalCode code)
  | otherwise = Nothing
```

### 2. Make Illegal States Unrepresentable

```haskell
-- ❌ BAD: Can create invalid money
data Money = Money
  { amount :: Double  -- Can be negative!
  , currency :: Text  -- Can be invalid!
  }

-- ✅ GOOD: Type-safe construction
data Money = Money
  { amount :: Scientific  -- High precision
  , currency :: Currency  -- Enum of valid currencies
  }

mkMoney :: Scientific -> Currency -> Maybe Money
mkMoney amt curr
  | amt >= 0 = Just (Money amt curr)
  | otherwise = Nothing
```

### 3. Provide Useful Operations

```haskell
data DateRange = DateRange Date Date

-- Useful operations
inRange :: Date -> DateRange -> Bool
duration :: DateRange -> Days
overlaps :: DateRange -> DateRange -> Bool
union :: DateRange -> DateRange -> Maybe DateRange
intersection :: DateRange -> DateRange -> Maybe DateRange
```

### 4. Implement Proper Equality

```haskell
-- Derive Eq for structural equality
data Money = Money Scientific Currency
  deriving (Eq)

-- Custom equality if needed
instance Eq PersonName where
  (PersonName f1 g1 _) == (PersonName f2 g2 _) =
    f1 == f2 && g1 == g2  -- Ignore middle name
```

### 5. Make Value Objects Serializable

```haskell
-- JSON serialization
instance ToJSON Money where
  toJSON (Money amt curr) = object
    [ "amount" .= amt
    , "currency" .= show curr
    ]

instance FromJSON Money where
  parseJSON = withObject "Money" $ \o -> do
    amt <- o .: "amount"
    curr <- o .: "currency"
    case mkMoney amt curr of
      Just money -> pure money
      Nothing -> fail "Invalid money value"
```

## Testing Value Objects

```haskell
spec :: Spec
spec = describe "PostalCode" $ do
  it "accepts valid postal code" $ do
    mkPostalCode "123-4567" `shouldSatisfy` isJust

  it "rejects invalid postal code" $ do
    mkPostalCode "12-34567" `shouldBe` Nothing
    mkPostalCode "abcd-efgh" `shouldBe` Nothing

  it "formats correctly" $ do
    let Just code = mkPostalCode "123-4567"
    formatPostalCode code `shouldBe` "〒123-4567"

-- Property-based tests
prop_postal_code_roundtrip :: Text -> Property
prop_postal_code_roundtrip input =
  validPostalCode input ==>
    let Just code = mkPostalCode input
    in unPostalCode code === input
```

## Summary

Value objects provide:
- **Type Safety**: Invalid states are unrepresentable
- **Immutability**: Thread-safe by default
- **Clarity**: Domain concepts are explicit
- **Validation**: Business rules enforced at construction
- **Reusability**: Shared across aggregates and contexts
