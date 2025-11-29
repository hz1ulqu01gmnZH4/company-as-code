namespace CompanyAsCode.SharedKernel

open System
open System.Text.RegularExpressions

/// Japanese-specific value objects for business domain
module Japanese =

    // ============================================
    // Corporate Number (法人番号)
    // ============================================

    /// 13-digit corporate number assigned by National Tax Agency
    /// Format: 1 check digit + 12 base digits
    type CorporateNumber = private CorporateNumber of string

    module CorporateNumber =

        /// Validate checksum using modulus 9 algorithm
        let private validateChecksum (digits: string) : bool =
            if digits.Length <> 13 then false
            else
                let baseDigits = digits.Substring(1)
                let checkDigit = int (digits.[0].ToString())

                // Weights alternate 1, 2 from right to left
                let weights = [| 1; 2; 1; 2; 1; 2; 1; 2; 1; 2; 1; 2 |]

                let sum =
                    baseDigits
                    |> Seq.mapi (fun i c -> int (c.ToString()) * weights.[11 - i])
                    |> Seq.sum

                let remainder = sum % 9
                let expectedCheck = if remainder = 0 then 0 else 9 - remainder
                checkDigit = expectedCheck

        /// Create a validated corporate number
        let create (value: string) : Result<CorporateNumber, string> =
            let cleaned = value.Replace("-", "").Replace(" ", "")

            if String.IsNullOrWhiteSpace(cleaned) then
                Error "Corporate number cannot be empty"
            elif cleaned.Length <> 13 then
                Error $"Corporate number must be 13 digits, got {cleaned.Length}"
            elif not (cleaned |> Seq.forall Char.IsDigit) then
                Error "Corporate number must contain only digits"
            elif not (validateChecksum cleaned) then
                Error "Corporate number checksum is invalid"
            else
                Ok (CorporateNumber cleaned)

        /// Create without validation (for testing/migration)
        let createUnsafe (value: string) : CorporateNumber =
            CorporateNumber value

        let value (CorporateNumber v) = v

        /// Format as XXXX-XX-XXXXXX
        let format (CorporateNumber v) =
            $"{v.Substring(0, 4)}-{v.Substring(4, 2)}-{v.Substring(6, 6)}"

    // ============================================
    // Prefecture (都道府県)
    // ============================================

    /// Japanese prefectures - all 47 as sum type for exhaustive matching
    type Prefecture =
        // Hokkaido & Tohoku
        | Hokkaido      // 北海道
        | Aomori        // 青森県
        | Iwate         // 岩手県
        | Miyagi        // 宮城県
        | Akita         // 秋田県
        | Yamagata      // 山形県
        | Fukushima     // 福島県
        // Kanto
        | Ibaraki       // 茨城県
        | Tochigi       // 栃木県
        | Gunma         // 群馬県
        | Saitama       // 埼玉県
        | Chiba         // 千葉県
        | Tokyo         // 東京都
        | Kanagawa      // 神奈川県
        // Chubu
        | Niigata       // 新潟県
        | Toyama        // 富山県
        | Ishikawa      // 石川県
        | Fukui         // 福井県
        | Yamanashi     // 山梨県
        | Nagano        // 長野県
        | Gifu          // 岐阜県
        | Shizuoka      // 静岡県
        | Aichi         // 愛知県
        // Kinki
        | Mie           // 三重県
        | Shiga         // 滋賀県
        | Kyoto         // 京都府
        | Osaka         // 大阪府
        | Hyogo         // 兵庫県
        | Nara          // 奈良県
        | Wakayama      // 和歌山県
        // Chugoku
        | Tottori       // 鳥取県
        | Shimane       // 島根県
        | Okayama       // 岡山県
        | Hiroshima     // 広島県
        | Yamaguchi     // 山口県
        // Shikoku
        | Tokushima     // 徳島県
        | Kagawa        // 香川県
        | Ehime         // 愛媛県
        | Kochi         // 高知県
        // Kyushu & Okinawa
        | Fukuoka       // 福岡県
        | Saga          // 佐賀県
        | Nagasaki      // 長崎県
        | Kumamoto      // 熊本県
        | Oita         // 大分県
        | Miyazaki      // 宮崎県
        | Kagoshima     // 鹿児島県
        | Okinawa       // 沖縄県

    module Prefecture =

        /// Get Japanese name of prefecture
        let toJapanese = function
            | Hokkaido -> "北海道"
            | Aomori -> "青森県"
            | Iwate -> "岩手県"
            | Miyagi -> "宮城県"
            | Akita -> "秋田県"
            | Yamagata -> "山形県"
            | Fukushima -> "福島県"
            | Ibaraki -> "茨城県"
            | Tochigi -> "栃木県"
            | Gunma -> "群馬県"
            | Saitama -> "埼玉県"
            | Chiba -> "千葉県"
            | Tokyo -> "東京都"
            | Kanagawa -> "神奈川県"
            | Niigata -> "新潟県"
            | Toyama -> "富山県"
            | Ishikawa -> "石川県"
            | Fukui -> "福井県"
            | Yamanashi -> "山梨県"
            | Nagano -> "長野県"
            | Gifu -> "岐阜県"
            | Shizuoka -> "静岡県"
            | Aichi -> "愛知県"
            | Mie -> "三重県"
            | Shiga -> "滋賀県"
            | Kyoto -> "京都府"
            | Osaka -> "大阪府"
            | Hyogo -> "兵庫県"
            | Nara -> "奈良県"
            | Wakayama -> "和歌山県"
            | Tottori -> "鳥取県"
            | Shimane -> "島根県"
            | Okayama -> "岡山県"
            | Hiroshima -> "広島県"
            | Yamaguchi -> "山口県"
            | Tokushima -> "徳島県"
            | Kagawa -> "香川県"
            | Ehime -> "愛媛県"
            | Kochi -> "高知県"
            | Fukuoka -> "福岡県"
            | Saga -> "佐賀県"
            | Nagasaki -> "長崎県"
            | Kumamoto -> "熊本県"
            | Oita -> "大分県"
            | Miyazaki -> "宮崎県"
            | Kagoshima -> "鹿児島県"
            | Okinawa -> "沖縄県"

        /// Get JIS code (2-digit)
        let toJisCode = function
            | Hokkaido -> "01"
            | Aomori -> "02"
            | Iwate -> "03"
            | Miyagi -> "04"
            | Akita -> "05"
            | Yamagata -> "06"
            | Fukushima -> "07"
            | Ibaraki -> "08"
            | Tochigi -> "09"
            | Gunma -> "10"
            | Saitama -> "11"
            | Chiba -> "12"
            | Tokyo -> "13"
            | Kanagawa -> "14"
            | Niigata -> "15"
            | Toyama -> "16"
            | Ishikawa -> "17"
            | Fukui -> "18"
            | Yamanashi -> "19"
            | Nagano -> "20"
            | Gifu -> "21"
            | Shizuoka -> "22"
            | Aichi -> "23"
            | Mie -> "24"
            | Shiga -> "25"
            | Kyoto -> "26"
            | Osaka -> "27"
            | Hyogo -> "28"
            | Nara -> "29"
            | Wakayama -> "30"
            | Tottori -> "31"
            | Shimane -> "32"
            | Okayama -> "33"
            | Hiroshima -> "34"
            | Yamaguchi -> "35"
            | Tokushima -> "36"
            | Kagawa -> "37"
            | Ehime -> "38"
            | Kochi -> "39"
            | Fukuoka -> "40"
            | Saga -> "41"
            | Nagasaki -> "42"
            | Kumamoto -> "43"
            | Oita -> "44"
            | Miyazaki -> "45"
            | Kagoshima -> "46"
            | Okinawa -> "47"

        /// Parse from JIS code
        let fromJisCode (code: string) : Prefecture option =
            match code with
            | "01" -> Some Hokkaido
            | "02" -> Some Aomori
            | "03" -> Some Iwate
            | "04" -> Some Miyagi
            | "05" -> Some Akita
            | "06" -> Some Yamagata
            | "07" -> Some Fukushima
            | "08" -> Some Ibaraki
            | "09" -> Some Tochigi
            | "10" -> Some Gunma
            | "11" -> Some Saitama
            | "12" -> Some Chiba
            | "13" -> Some Tokyo
            | "14" -> Some Kanagawa
            | "15" -> Some Niigata
            | "16" -> Some Toyama
            | "17" -> Some Ishikawa
            | "18" -> Some Fukui
            | "19" -> Some Yamanashi
            | "20" -> Some Nagano
            | "21" -> Some Gifu
            | "22" -> Some Shizuoka
            | "23" -> Some Aichi
            | "24" -> Some Mie
            | "25" -> Some Shiga
            | "26" -> Some Kyoto
            | "27" -> Some Osaka
            | "28" -> Some Hyogo
            | "29" -> Some Nara
            | "30" -> Some Wakayama
            | "31" -> Some Tottori
            | "32" -> Some Shimane
            | "33" -> Some Okayama
            | "34" -> Some Hiroshima
            | "35" -> Some Yamaguchi
            | "36" -> Some Tokushima
            | "37" -> Some Kagawa
            | "38" -> Some Ehime
            | "39" -> Some Kochi
            | "40" -> Some Fukuoka
            | "41" -> Some Saga
            | "42" -> Some Nagasaki
            | "43" -> Some Kumamoto
            | "44" -> Some Oita
            | "45" -> Some Miyazaki
            | "46" -> Some Kagoshima
            | "47" -> Some Okinawa
            | _ -> None

    // ============================================
    // Corporate Seals (会社印)
    // ============================================

    /// Types of corporate seals used in Japan
    type SealType =
        | Jituin        // 実印 - Registered company seal (代表印)
        | Ginkoin       // 銀行印 - Bank seal
        | Kakuin        // 角印 - Square acknowledgment seal
        | Mitomein      // 認印 - Personal acknowledgment seal

    /// Corporate seal registration status
    type SealRegistrationStatus =
        | Registered of registrationDate: DateTimeOffset * legalAffairsBureau: string
        | Unregistered
        | Retired of retirementDate: DateTimeOffset

    /// Corporate seal value object
    type CorporateSeal = {
        SealType: SealType
        Status: SealRegistrationStatus
        Description: string option
    }

    module CorporateSeal =

        let createRegistered (sealType: SealType) (registrationDate: DateTimeOffset) (bureau: string) =
            {
                SealType = sealType
                Status = Registered (registrationDate, bureau)
                Description = None
            }

        let isRegistered seal =
            match seal.Status with
            | Registered _ -> true
            | _ -> false

        let retire seal retirementDate =
            { seal with Status = Retired retirementDate }

    // ============================================
    // Postal Code (郵便番号)
    // ============================================

    /// Japanese postal code (7 digits, format: XXX-XXXX)
    type PostalCode = private PostalCode of string

    module PostalCode =

        let private pattern = Regex(@"^\d{3}-?\d{4}$", RegexOptions.Compiled)

        let create (value: string) : Result<PostalCode, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Postal code cannot be empty"
            elif not (pattern.IsMatch(value)) then
                Error "Postal code must be 7 digits (format: XXX-XXXX or XXXXXXX)"
            else
                let cleaned = value.Replace("-", "")
                Ok (PostalCode cleaned)

        let value (PostalCode v) = v

        /// Format as XXX-XXXX
        let format (PostalCode v) =
            $"{v.Substring(0, 3)}-{v.Substring(3, 4)}"

    // ============================================
    // Japanese Era Calendar (元号)
    // ============================================

    /// Japanese era (gengo)
    type JapaneseEra =
        | Meiji     // 明治 (1868-1912)
        | Taisho    // 大正 (1912-1926)
        | Showa     // 昭和 (1926-1989)
        | Heisei    // 平成 (1989-2019)
        | Reiwa     // 令和 (2019-)

    module JapaneseEra =

        let toKanji = function
            | Meiji -> "明治"
            | Taisho -> "大正"
            | Showa -> "昭和"
            | Heisei -> "平成"
            | Reiwa -> "令和"

        let startYear = function
            | Meiji -> 1868
            | Taisho -> 1912
            | Showa -> 1926
            | Heisei -> 1989
            | Reiwa -> 2019

        /// Convert Gregorian date to Japanese era date
        let fromGregorian (date: DateTime) : (JapaneseEra * int) option =
            let year = date.Year
            if year >= 2019 then Some (Reiwa, year - 2018)
            elif year >= 1989 then Some (Heisei, year - 1988)
            elif year >= 1926 then Some (Showa, year - 1925)
            elif year >= 1912 then Some (Taisho, year - 1911)
            elif year >= 1868 then Some (Meiji, year - 1867)
            else None

        /// Format date in Japanese era format
        let formatDate (date: DateTime) : string =
            match fromGregorian date with
            | Some (era, year) ->
                let eraName = toKanji era
                let yearStr = if year = 1 then "元" else string year
                $"{eraName}{yearStr}年{date.Month}月{date.Day}日"
            | None -> date.ToString("yyyy年M月d日")

    // ============================================
    // Bilingual Name (日英名称)
    // ============================================

    /// Bilingual name supporting Japanese and English
    type BilingualName = {
        Japanese: string
        JapaneseKana: string option  // Furigana/reading
        English: string option
    }

    module BilingualName =

        let create (japanese: string) : Result<BilingualName, string> =
            if String.IsNullOrWhiteSpace(japanese) then
                Error "Japanese name is required"
            else
                Ok {
                    Japanese = japanese
                    JapaneseKana = None
                    English = None
                }

        let withKana (kana: string) (name: BilingualName) =
            { name with JapaneseKana = Some kana }

        let withEnglish (english: string) (name: BilingualName) =
            { name with English = Some english }

        let display (name: BilingualName) =
            match name.English with
            | Some eng -> $"{name.Japanese} ({eng})"
            | None -> name.Japanese

    // ============================================
    // Company Entity Types (会社の種類)
    // ============================================

    /// Types of companies under Japanese Companies Act
    type EntityType =
        | KabushikiKaisha   // 株式会社 - Stock Company (K.K.)
        | GodoKaisha        // 合同会社 - Limited Liability Company (G.K.)
        | GomeiKaisha       // 合名会社 - General Partnership
        | GoshiKaisha       // 合資会社 - Limited Partnership

    module EntityType =

        let toJapanese = function
            | KabushikiKaisha -> "株式会社"
            | GodoKaisha -> "合同会社"
            | GomeiKaisha -> "合名会社"
            | GoshiKaisha -> "合資会社"

        let toAbbreviation = function
            | KabushikiKaisha -> "K.K."
            | GodoKaisha -> "G.K."
            | GomeiKaisha -> "Gomei"
            | GoshiKaisha -> "Goshi"

        let toEnglish = function
            | KabushikiKaisha -> "Stock Company"
            | GodoKaisha -> "Limited Liability Company"
            | GomeiKaisha -> "General Partnership Company"
            | GoshiKaisha -> "Limited Partnership Company"

        /// Minimum legal capital requirement
        let minimumCapital = function
            | KabushikiKaisha -> 1m  // ¥1 legal minimum
            | GodoKaisha -> 1m       // ¥1 legal minimum
            | GomeiKaisha -> 0m      // No minimum
            | GoshiKaisha -> 0m      // No minimum

        /// Practical recommended capital
        let recommendedCapital = function
            | KabushikiKaisha -> 10_000_000m  // ¥10M recommended
            | GodoKaisha -> 3_000_000m        // ¥3M recommended
            | GomeiKaisha -> 0m
            | GoshiKaisha -> 0m

        /// Whether entity type has limited liability for all members
        let hasLimitedLiability = function
            | KabushikiKaisha -> true
            | GodoKaisha -> true
            | GomeiKaisha -> false  // Unlimited liability
            | GoshiKaisha -> false  // Mixed (some unlimited)

        /// Required registration fee (登録免許税)
        let registrationFee capital = function
            | KabushikiKaisha -> max 150_000m (capital * 0.007m)
            | GodoKaisha -> max 60_000m (capital * 0.007m)
            | GomeiKaisha -> 60_000m
            | GoshiKaisha -> 60_000m
