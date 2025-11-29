namespace CompanyAsCode.SharedKernel

open System
open System.Text.RegularExpressions

/// Contact information value objects
module Contact =

    // ============================================
    // Person Name (氏名)
    // ============================================

    /// Japanese person name with kanji and furigana
    type PersonName = {
        FamilyName: string          // 姓 (Sei)
        GivenName: string           // 名 (Mei)
        FamilyNameKana: string option   // 姓のふりがな
        GivenNameKana: string option    // 名のふりがな
    }

    module PersonName =

        let create (familyName: string) (givenName: string) : Result<PersonName, string> =
            if String.IsNullOrWhiteSpace(familyName) then
                Error "Family name is required"
            elif String.IsNullOrWhiteSpace(givenName) then
                Error "Given name is required"
            else
                Ok {
                    FamilyName = familyName.Trim()
                    GivenName = givenName.Trim()
                    FamilyNameKana = None
                    GivenNameKana = None
                }

        let withKana (familyKana: string) (givenKana: string) (name: PersonName) =
            { name with
                FamilyNameKana = Some familyKana
                GivenNameKana = Some givenKana }

        /// Full name in Japanese order (family name first)
        let fullName (name: PersonName) : string =
            $"{name.FamilyName} {name.GivenName}"

        /// Full name in Western order (given name first)
        let fullNameWestern (name: PersonName) : string =
            $"{name.GivenName} {name.FamilyName}"

        /// Full name with furigana
        let fullNameWithKana (name: PersonName) : string =
            match name.FamilyNameKana, name.GivenNameKana with
            | Some fk, Some gk -> $"{name.FamilyName}（{fk}） {name.GivenName}（{gk}）"
            | _ -> fullName name

    // ============================================
    // Address (住所)
    // ============================================

    /// Japanese address structure
    type Address = {
        PostalCode: Japanese.PostalCode
        Prefecture: Japanese.Prefecture
        City: string              // 市区町村
        Street: string            // 町名・番地
        Building: string option   // 建物名・部屋番号
    }

    module Address =

        let create
            (postalCode: Japanese.PostalCode)
            (prefecture: Japanese.Prefecture)
            (city: string)
            (street: string)
            : Result<Address, string> =

            if String.IsNullOrWhiteSpace(city) then
                Error "City is required"
            elif String.IsNullOrWhiteSpace(street) then
                Error "Street address is required"
            else
                Ok {
                    PostalCode = postalCode
                    Prefecture = prefecture
                    City = city.Trim()
                    Street = street.Trim()
                    Building = None
                }

        let withBuilding (building: string) (address: Address) =
            { address with Building = Some building }

        /// Format as single line
        let format (addr: Address) : string =
            let building = addr.Building |> Option.map (fun b -> $" {b}") |> Option.defaultValue ""
            $"〒{Japanese.PostalCode.format addr.PostalCode} {Japanese.Prefecture.toJapanese addr.Prefecture}{addr.City}{addr.Street}{building}"

        /// Format as multi-line
        let formatMultiLine (addr: Address) : string =
            let lines = [
                $"〒{Japanese.PostalCode.format addr.PostalCode}"
                $"{Japanese.Prefecture.toJapanese addr.Prefecture}{addr.City}"
                addr.Street
            ]
            let withBuilding =
                match addr.Building with
                | Some b -> lines @ [b]
                | None -> lines
            String.Join("\n", withBuilding)

    // ============================================
    // Email (メールアドレス)
    // ============================================

    /// Email address value object
    type Email = private Email of string

    module Email =

        // Simple email pattern - not exhaustive but practical
        let private pattern =
            Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)

        let create (value: string) : Result<Email, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Email cannot be empty"
            else
                let trimmed = value.Trim().ToLowerInvariant()
                if pattern.IsMatch(trimmed) then
                    Ok (Email trimmed)
                else
                    Error $"Invalid email format: {value}"

        let value (Email e) = e

        let domain (Email e) =
            e.Split('@').[1]

    // ============================================
    // Phone Number (電話番号)
    // ============================================

    /// Phone number type
    type PhoneType =
        | Mobile    // 携帯電話
        | Landline  // 固定電話
        | Fax       // FAX
        | Toll      // フリーダイヤル

    /// Phone number value object
    type PhoneNumber = private {
        _number: string
        _type: PhoneType
    }

    module PhoneNumber =

        // Japanese phone patterns
        let private mobilePattern = Regex(@"^0[789]0-?\d{4}-?\d{4}$", RegexOptions.Compiled)
        let private landlinePattern = Regex(@"^0\d{1,4}-?\d{1,4}-?\d{4}$", RegexOptions.Compiled)
        let private tollFreePattern = Regex(@"^0120-?\d{3}-?\d{3}$", RegexOptions.Compiled)

        let create (value: string) (phoneType: PhoneType) : Result<PhoneNumber, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Phone number cannot be empty"
            else
                let cleaned = value.Replace("-", "").Replace(" ", "")
                let isValid =
                    match phoneType with
                    | Mobile -> mobilePattern.IsMatch(value) || cleaned.Length = 11
                    | Landline -> landlinePattern.IsMatch(value) || (cleaned.Length >= 10 && cleaned.Length <= 11)
                    | Fax -> landlinePattern.IsMatch(value) || (cleaned.Length >= 10 && cleaned.Length <= 11)
                    | Toll -> tollFreePattern.IsMatch(value) || cleaned.Length = 10

                if isValid then
                    Ok { _number = cleaned; _type = phoneType }
                else
                    Error $"Invalid phone number format for {phoneType}: {value}"

        let value (pn: PhoneNumber) = pn._number
        let phoneType (pn: PhoneNumber) = pn._type

        /// Format with hyphens
        let format (pn: PhoneNumber) : string =
            let n = pn._number
            match pn._type with
            | Mobile when n.Length = 11 ->
                $"{n.Substring(0,3)}-{n.Substring(3,4)}-{n.Substring(7,4)}"
            | Toll when n.Length = 10 ->
                $"{n.Substring(0,4)}-{n.Substring(4,3)}-{n.Substring(7,3)}"
            | _ when n.Length = 10 ->
                $"{n.Substring(0,3)}-{n.Substring(3,3)}-{n.Substring(6,4)}"
            | _ -> n

    // ============================================
    // Website URL
    // ============================================

    /// Website URL value object
    type WebsiteUrl = private WebsiteUrl of Uri

    module WebsiteUrl =

        let create (value: string) : Result<WebsiteUrl, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "URL cannot be empty"
            else
                match Uri.TryCreate(value, UriKind.Absolute) with
                | true, uri when uri.Scheme = "http" || uri.Scheme = "https" ->
                    Ok (WebsiteUrl uri)
                | _ ->
                    // Try adding https://
                    match Uri.TryCreate($"https://{value}", UriKind.Absolute) with
                    | true, uri -> Ok (WebsiteUrl uri)
                    | _ -> Error $"Invalid URL: {value}"

        let value (WebsiteUrl uri) = uri
        let toString (WebsiteUrl uri) = uri.ToString()

    // ============================================
    // Contact Info (連絡先)
    // ============================================

    /// Complete contact information
    type ContactInfo = {
        PrimaryEmail: Email option
        SecondaryEmail: Email option
        Phone: PhoneNumber option
        Mobile: PhoneNumber option
        Fax: PhoneNumber option
        Website: WebsiteUrl option
        Address: Address option
    }

    module ContactInfo =

        let empty : ContactInfo = {
            PrimaryEmail = None
            SecondaryEmail = None
            Phone = None
            Mobile = None
            Fax = None
            Website = None
            Address = None
        }

        let withEmail (email: Email) (info: ContactInfo) =
            { info with PrimaryEmail = Some email }

        let withPhone (phone: PhoneNumber) (info: ContactInfo) =
            { info with Phone = Some phone }

        let withMobile (mobile: PhoneNumber) (info: ContactInfo) =
            { info with Mobile = Some mobile }

        let withAddress (address: Address) (info: ContactInfo) =
            { info with Address = Some address }

        let withWebsite (url: WebsiteUrl) (info: ContactInfo) =
            { info with Website = Some url }

    // ============================================
    // Representative (代表者)
    // ============================================

    /// Title/position of representative
    type RepresentativeTitle =
        | President                 // 代表取締役社長
        | ChairmanAndPresident      // 代表取締役会長
        | VicePresident             // 代表取締役副社長
        | Director                  // 取締役
        | ExecutiveOfficer          // 執行役
        | Partner                   // 代表社員 (for G.K.)
        | Other of string

    /// Representative person
    type Representative = {
        Name: PersonName
        Title: RepresentativeTitle
        Contact: ContactInfo
    }

    module Representative =

        let create (name: PersonName) (title: RepresentativeTitle) : Representative =
            {
                Name = name
                Title = title
                Contact = ContactInfo.empty
            }

        let withContact (contact: ContactInfo) (rep: Representative) =
            { rep with Contact = contact }

        let titleToJapanese = function
            | President -> "代表取締役社長"
            | ChairmanAndPresident -> "代表取締役会長"
            | VicePresident -> "代表取締役副社長"
            | Director -> "取締役"
            | ExecutiveOfficer -> "執行役"
            | Partner -> "代表社員"
            | Other title -> title
