# Japanese Companies Act (会社法) - Comprehensive Research Report

**Research Date:** 2025-11-28
**Primary Legislation:** Companies Act (Act No. 86 of 2005)
**Purpose:** Domain-Driven Design modeling for company structure implementation

## Table of Contents

1. [Legal Framework Overview](#legal-framework-overview)
2. [Company Types and Legal Requirements](#company-types-and-legal-requirements)
3. [Corporate Governance Structures](#corporate-governance-structures)
4. [Legal Compliance Requirements](#legal-compliance-requirements)
5. [Business Operations Framework](#business-operations-framework)
6. [Recent Amendments (2019-2022)](#recent-amendments-2019-2022)
7. [Domain Model Mapping](#domain-model-mapping)

---

## Legal Framework Overview

### Primary Legislation

The **Japanese Companies Act (会社法, Kaisha-hō)** is **Act No. 86 of 2005**, serving as the primary legal framework governing business entities in Japan. The formation, organization, operation and management of companies are governed by the provisions of this Act, except as otherwise provided by other acts.

**Key Related Legislation:**
- **Financial Instruments and Exchange Act (FIEA)** - Act No. 25 of 1948 (for listed companies)
- **Commercial Registration Act** - Governs registration requirements at Legal Affairs Bureau
- **Corporate Governance Code** - Soft law for listed companies (Tokyo Stock Exchange)
- **Foreign Exchange and Foreign Trade Act (FEFTA)** - Governs foreign direct investment

### Fundamental Principles

1. **Corporate Separate Personality** - Companies are legal entities separate from shareholders
2. **Limited Liability** - Shareholder liability limited to capital contribution
3. **Shareholder Equality** - Equal treatment based on share class and number
4. **Corporate Governance** - Mandatory governance structures based on company type
5. **Capital Maintenance** - Strict rules protecting creditor interests

---

## Company Types and Legal Requirements

### 1. Kabushiki Kaisha (株式会社) - Stock Company (K.K.)

**Overview:**
The most common and prestigious form of corporation in Japan, comparable to a corporation in Western countries. Traditionally more prevalent than other forms, especially for companies expecting significant growth.

**Capital Requirements:**
- **Minimum Capital:** Technically 1 JPY under the Companies Act, but practical minimums apply:
  - Recommended minimum: JPY 1,000,000 for credibility with landlords and banks
  - For visa sponsorship: JPY 5,000,000+ required
  - Business Manager visa (foreign nationals): JPY 30,000,000 investment required
- **Capital raise obligation:** Must raise capital to JPY 10,000,000 within 5 years of incorporation

**Formation Requirements:**
- **Minimum Directors:** At least 1 director (3+ if establishing Board of Directors)
- **Shareholders:** Minimum 1 shareholder
- **Articles of Incorporation:** Must be notarized by Japanese notary public
- **Corporate Seal (会社印):** Representative seal (代表印) must be registered at Legal Affairs Bureau

**Formation Costs:**
- Registration license tax: JPY 150,000 minimum (or 0.7% of stated capital, whichever is higher)
- Notary fees for Articles of Incorporation
- Total approximate cost: JPY 200,000-250,000 (excluding capital)

**Governance Structure:**
- **Minimum Structure:** General shareholders' meeting + at least 1 director
- **With Board:** General shareholders' meeting + Board of Directors (3+ directors) + Statutory Auditor(s)
- **Advanced Structures:**
  - Company with Audit and Supervisory Committee
  - Company with Three Committees (Nominating, Audit, Compensation)

**Characteristics:**
- Can issue multiple classes of shares
- Can establish Board of Directors with committees
- Suitable for fundraising and significant business expansion
- More complex compliance and administrative requirements
- Higher setup costs but greater prestige and credibility

### 2. Godo Kaisha (合同会社) - Limited Liability Company (G.K.)

**Overview:**
Introduced in 2006, modeled after the U.S. LLC. Increasingly popular among foreign companies establishing wholly-owned subsidiaries due to simplicity, flexibility, and lower costs.

**Capital Requirements:**
- **Minimum Capital:** 1 JPY (no legal minimum)
- **Practical minimum:** JPY 1,000,000 recommended for credibility
- **For visa sponsorship:** JPY 5,000,000+ required
- **Capital raise obligation:** Must raise to JPY 3,000,000 within 5 years

**Formation Requirements:**
- **Members:** Minimum 1 member (investors/managers are typically the same)
- **Directors:** Not required (members manage directly)
- **Articles of Incorporation:** No notarization required
- **Corporate Seal:** Representative seal must be registered

**Formation Costs:**
- Registration license tax: JPY 60,000 (or 0.7% of stated capital, whichever is higher)
- No notary fees required
- Total approximate cost: JPY 100,000+ (excluding capital)
- Significantly cheaper than K.K.

**Governance Structure:**
- **Default:** All members participate in management
- **Flexibility:** Can designate specific members as executive members
- **No Board Required:** Simplified governance structure
- **No Statutory Auditors Required**

**Characteristics:**
- Liability of members limited to capital contributions
- No separation between shareholders and managers (unless specified)
- Cannot issue shares or raise capital through public offerings
- Simplified reporting and compliance requirements
- Lower prestige than K.K. but increasing acceptance
- Ideal for small businesses, startups, and wholly-owned subsidiaries

### 3. Gomei Kaisha (合名会社) - General Partnership Company

**Overview:**
Rarely used due to unlimited liability of all members. Has legal personality.

**Key Characteristics:**
- **Liability:** All members have unlimited liability
- **Management:** All members participate in management
- **Legal Status:** Has corporate legal personality
- **Usage:** Extremely rare in modern Japan

### 4. Goshi Kaisha (合資会社) - Limited Partnership Company

**Overview:**
Consists of both unlimited and limited liability members. Roughly equivalent to a limited partnership.

**Key Characteristics:**
- **Member Types:**
  - At least 1 unlimited liability member (manages the company)
  - At least 1 limited liability member (silent partner)
- **Legal Status:** Has corporate legal personality
- **Usage:** Uncommon; mostly historical

### Public vs. Private Companies

**Public Company (公開会社):**
- Definition: Company where ALL shares can be transferred without company approval
- Additional requirements for governance and disclosure
- Must have Board of Directors

**Private Company (非公開会社):**
- Definition: Company with transfer restrictions on ALL shares in articles of incorporation
- Can have simplified governance structures
- More flexibility in shareholder treatment

---

## Corporate Governance Structures

### Overview of Governance Models

Under Japan's Companies Act, the management structure can be classified into **three primary types**:

1. **Company with Statutory Auditor(s)** (監査役設置会社) - ~55-57% of listed companies
2. **Company with Audit and Supervisory Committee** (監査等委員会設置会社) - ~41-43% of listed companies
3. **Company with Three Committees** (指名委員会等設置会社) - ~2% of listed companies

### 1. Company with Statutory Auditor(s)

**Structure:**
- **Board of Directors (取締役会):** Minimum 3 directors
  - Listed companies: Must have 1+ outside directors
  - Large private companies: May require outside directors
- **Statutory Auditors (監査役):** Minimum 1 auditor
  - For Board of Statutory Auditors: Minimum 3 auditors (majority must be outside auditors)
- **Shareholders' Meeting (株主総会):** Supreme decision-making body

**Key Features:**
- **Separation of Functions:** Directors manage, auditors supervise
- **Auditor Authority:**
  - Attend board meetings
  - Audit directors' conduct
  - Approve appointment/compensation of accounting auditor
  - Can initiate lawsuits against directors
- **Board Authority:**
  - Supervises directors' execution of duties
  - Decides fundamental management policies
  - Appoints representative directors

**Applicable Companies:**
- Traditional structure, most common in Japan
- Default for most established K.K. companies
- Required for companies not adopting alternative structures

### 2. Company with Audit and Supervisory Committee

**Introduced:** May 2015 (Companies Act Amendment 2014)
**Growth:** Rapidly increasing adoption (41-43% of listed companies)

**Structure:**
- **Board of Directors:** Includes audit committee members
  - Minimum 3 directors on Audit and Supervisory Committee
  - Majority of committee must be outside directors
- **Audit and Supervisory Committee:** Part of board structure
- **No Statutory Auditors:** Replaced by committee structure

**Key Features:**
- **Unified Board:** Committee members are directors (unlike statutory auditors)
- **No Nominating/Compensation Committees Required:** Unlike three-committee structure
- **Flexibility:** Can delegate substantial authority to directors
- **Modern Structure:** Designed to enhance governance while reducing complexity

**Advantages over Traditional Structure:**
- Enhanced board oversight through director-level audit committee
- Greater flexibility in executive delegation
- Lower complexity than three-committee model
- Meets international governance standards

**Advantages over Three-Committee Structure:**
- No requirement for nominating or compensation committees
- No mandatory executive officer (執行役) appointments
- Easier transition from traditional structure

### 3. Company with Three Committees (Nominating Committee, etc.)

**Structure:**
- **Board of Directors:** Strategic oversight and supervision
- **Three Mandatory Committees:**
  1. **Nominating Committee (指名委員会):** Decides director nomination/dismissal agenda
  2. **Audit Committee (監査委員会):** Audits executive officers and directors
  3. **Compensation Committee (報酬委員会):** Determines individual compensation
- **Executive Officers (執行役):** Separate from directors, execute business operations
  - Minimum 1 executive officer
  - At least 1 must be representative executive officer

**Committee Requirements:**
- Each committee: Minimum 3 directors
- **Majority must be outside directors** for each committee
- Cannot delegate committee responsibilities

**Key Features:**
- **Strict Separation:** Supervision (board) vs. execution (officers)
- **Outside Director Driven:** Majority on all three committees
- **Executive Officers:** Day-to-day management by appointed officers
- **International Model:** Similar to U.S./U.K. governance structures

**Usage:**
- Primarily adopted by large multinationals
- Only ~2% of listed companies use this structure
- Complex but meets highest governance standards
- Obstacles: Requires finding qualified outside directors, complexity

### Outside Directors (社外取締役)

**Requirements:**
- **All Listed Companies:** Must have outside director(s)
- **Corporate Governance Code:**
  - Prime Market: 1/3+ of directors should be independent outside directors
  - Other Markets: 2+ independent outside directors minimum
- **Three-Committee Companies:** Majority on each committee must be outside directors
- **Audit Committee Companies:** Majority of audit committee must be outside directors

**Definition:**
Independent from company management, never employed as director/executive of company or subsidiaries, meets independence criteria.

### Directors (取締役)

**Appointment and Removal:**
- **Appointed by:** Shareholder resolution (majority vote if quorum met)
- **Term:** Maximum 2 years (can be shortened in articles)
  - Listed companies with audit committee: 1 year for non-committee directors
- **Removal:** Shareholder resolution (can require higher threshold in articles)

**Fiduciary Duties:**
1. **Duty of Care (善管注意義務):**
   - Manage with care of prudent manager
   - Derived from mandate relationship (Companies Act Art. 330, Civil Code Art. 644)
   - Requires reasonable information and diligent decision-making

2. **Duty of Loyalty (忠実義務):**
   - Perform duties loyally for company benefit (Art. 355)
   - Prohibits self-dealing
   - Prioritize company interests over personal/third-party interests

3. **Duty to Establish Internal Controls:**
   - Required for all directors
   - Ensure proper risk management and compliance

4. **Duty to Supervise:**
   - Monitor other directors' execution of duties
   - Part of fiduciary responsibility

**Liability:**
- **To Company:** Directors liable for damages if breach fiduciary duty
- **To Third Parties:** Liable for damages if grossly negligent or knowing breach
- **Derivative Suits:** Shareholders can sue on behalf of company
- **Limitation:** Can limit liability through articles (non-representative, non-executive directors)

**Business Judgment Rule:**
Directors given broad discretion in business decisions; not liable unless decision/process "significantly unreasonable."

**D&O Insurance:**
- Widely available in Japan
- Requires board approval (or shareholder approval if no board)
- Covered by 2019 amendments to Companies Act
- Can cover costs and damages from duty performance
- No conflict-of-interest transaction rules apply (special provisions enacted)

### Representative Director (代表取締役)

**Legal Status:**
- **Required for all K.K.:** Companies Act mandates representative director
- **Highest Authority:** Most senior executive managing the corporation
- **Public Registration:** Name and seal registered in official corporate register
- **Binding Authority:** Statements legally bind the corporation

**Appointment:**
- **By Board:** If company has Board of Directors, board appoints representative director(s)
- **Number:** Typically 1-3 representative directors depending on company size
- **Title:** Usually holds title of "President," "CEO," or "President and CEO" in English

**Duties:**
- Enter into business contracts on behalf of company
- Sign legal documents binding the company
- Manage day-to-day corporate operations
- Represent company in all legal matters

**Relationship to Other Roles:**
- **社長 (Shacho):** Informal title for same position (President)
- **CEO:** Translation used on business cards
- **Executive Officer (執行役):** Different role in three-committee companies

### Executive Officers (執行役) vs. Corporate Officers

**Executive Officers (執行役 Shikkō-yaku):**
- **Only in Three-Committee Companies**
- Elected and removed by Board of Directors
- Execute decisions made by committees
- Substantial decision-making authority delegated from board
- Can serve concurrently as directors
- Term: 1 year
- Representative Executive Officer: Appointed from executive officers, binds company

**Corporate Officers (執行役員 - not statutory):**
- Internal management position (not defined by Companies Act)
- No legal authority to bind company
- Administrative/operational role below board level
- Common in large Japanese companies

### Statutory Auditors (監査役)

**Appointment and Removal:**
- **Appointed by:** Shareholder majority vote
- **Removal:** Requires 2/3 shareholder vote (higher threshold than directors)
- **Term:** 4 years (longer than directors)
- **Independence:** Majority of Board of Statutory Auditors must be outside auditors

**Authority and Duties:**
- Supervise directors' conduct (not business decisions)
- Attend board meetings
- Investigate company's business and financial conditions
- Approve accounting auditor appointment, removal, compensation
- Can initiate lawsuits against directors
- Can request directors convene shareholder meetings

**Key Distinction from Audit Committee:**
- Statutory auditors are NOT directors
- Audit committee members ARE directors
- Statutory auditors have longer terms (4 years vs. 1-2 years for directors)

### Shareholders' Meeting (株主総会)

**Authority:**
- Supreme decision-making body of the company
- Decides matters reserved by Companies Act or articles of incorporation
- Cannot make decisions on matters delegated to board (if board exists)

**Types of Meetings:**
1. **Annual General Meeting:** Must be held within 3 months after fiscal year end
2. **Extraordinary General Meeting:** Called when needed

**Voting Requirements:**

**Ordinary Resolution:**
- **Quorum:** Majority of voting rights present (can be reduced/eliminated in articles for some matters)
  - Director election/dismissal: Minimum 1/3 quorum (cannot be eliminated)
- **Vote:** Majority of votes present
- **Matters:** Director appointment, dividend approval, etc.

**Extraordinary Resolution:**
- **Quorum:** Majority of voting rights present (can be reduced to minimum 1/3 in articles)
- **Vote:** 2/3 majority of votes present
- **Matters:**
  - Amendment of articles of incorporation
  - Mergers, acquisitions, major asset transfers
  - Issuance of new shares
  - Company splits, share exchanges, share transfers

**Special Majority Resolution:**
- **Requirements:**
  - (i) 1/2+ of shareholders (headcount) + 2/3 votes; OR
  - (ii) 1/2+ of all shareholders (headcount) + 3/4 votes
- **No Quorum Requirement**
- **Matters:** Special situations as defined in Companies Act

**Recent Developments (2019-2022 Amendments):**
- Electronic provision of shareholder meeting materials (mandatory for listed companies)
- Virtual shareholder meetings allowed under Industrial Competitiveness Enhancement Act (2021)
- Limitation on shareholder proposals (maximum 10 proposals)

---

## Legal Compliance Requirements

### Annual Reporting Requirements

#### 1. Under the Companies Act (All K.K. Companies)

**Required Documents:**
Following each fiscal year end, joint stock companies must prepare:

1. **Financial Statements (計算書類):**
   - Balance Sheet (貸借対照表)
   - Profit and Loss Statement (損益計算書)
   - Statement of Changes in Shareholders' Equity (株主資本等変動計算書)
   - Notes to Financial Statements

2. **Business Report (事業報告):**
   - Overview of business operations
   - Corporate governance structure
   - Director and auditor information

3. **Supplementary Statements:**
   - Additional supporting documentation for above

**Timeline:**
- **Preparation:** Within 3 months after fiscal year end
- **Shareholders' Meeting:** Must be held within 3 months after balance sheet date
- **Approval:** Financial statements require shareholder approval

**Audit Requirements:**
- **Large Companies:** Mandatory accounting audit
  - Definition: Capital ≥ JPY 500 million OR Total liabilities ≥ JPY 20 billion
- **Statutory Auditor Review:** Required if company has statutory auditors
- **Board Approval:** Required before shareholder submission

#### 2. Under the Financial Instruments and Exchange Act (Listed Companies)

**Annual Securities Report (有価証券報告書 Yūka shōken hōkokusho):**
- **Filing Requirement:** All listed companies
- **Deadline:** Within 3 months after fiscal year end
- **Filed With:** Financial Services Agency (FSA), local finance bureau
- **Contents:**
  - Company overview, business description
  - Facility and equipment information
  - Detailed financial statements
  - Corporate governance disclosures
  - Risk factors
  - Related party transactions

**Quarterly Reports:**
- **Frequency:** Every quarter (Q1, Q2, Q3)
- **Deadline:** 45 days after quarter end
- **Contents:** Interim financial information, material changes

**Internal Control Report:**
- **Frequency:** Once per fiscal year
- **Purpose:** Assessment of internal controls for financial statement reliability
- **Filed With:** Relevant local finance bureau

**Corporate Governance Report:**
- **Required By:** Tokyo Stock Exchange regulations (not FIEA)
- **Contents:**
  - Corporate governance system outline
  - Internal control system basic policy
  - Director, auditor, executive officer relationships
  - Compliance with Corporate Governance Code

#### 3. Accounting Standards

**Japanese GAAP (JGAAP):**
- Default standard for most companies
- Required for private companies
- Acceptable for listed companies (consolidated and standalone)

**International Financial Reporting Standards (IFRS):**
- **Mandatory:** For certain listed companies (consolidated statements)
- **Voluntary:** Can be adopted by public companies
- **Growing Adoption:** Increasing among multinationals

**US GAAP:**
- Permitted for companies cross-listed in United States

#### 4. ESG and Sustainability Disclosure

**Effective:** 2022-2023 (FIEA amendments)
**Requirement:** Publicly traded companies must disclose:
- Sustainability efforts and initiatives
- Environmental, Social, Governance (ESG) considerations
- Climate-related financial risks (TCFD framework encouraged)

**Tokyo Stock Exchange (Prime Market):**
- **English Disclosure Requirement (April 2025):**
  - All disclosures must be made in both Japanese and English
  - Applies to Prime Market listed companies

### Financial Disclosure and Transparency

#### Distributable Amount (配当可能額)

**Calculation:**
- Based on balance sheet at end of latest fiscal year
- Formula: Other capital surplus + Other retained earnings - Adjustments
- Key deductions: Treasury stock book value, certain liabilities

**Dividend Distribution Rules:**

1. **Net Asset Minimum:**
   - If net assets < JPY 3,000,000, NO dividends can be paid

2. **Reserve Requirements:**
   - When paying dividends, must set aside smaller of:
     - (i) 10% of surplus distributed, OR
     - (ii) Amount to bring capital reserve + profit reserve to 25% of share capital
   - Continues until reserves = 25% of share capital

3. **Decision Authority:**
   - **Default:** Shareholder resolution required
   - **Optional:** Articles can delegate to Board of Directors (if certain requirements met)
   - **Frequency:** Can issue dividends multiple times per fiscal year within distributable amount

4. **Director Liability:**
   - If dividends paid without distributable amount:
     - Directors liable to repay amount unless prove not negligent
     - Personal liability for illegal distributions

#### Capital Maintenance Rules

**Purpose:** Protect creditors by maintaining minimum capital

**Key Rules:**
1. **Minimum Capital:** JPY 1 (legally), JPY 3,000,000 (for dividend capacity)
2. **Treasury Stock:** Deducted from distributable amount
3. **Capital Reserves:** Must maintain certain levels before distributions
4. **Financial Statement Basis:** Distributable amount based on official financial statements

**Continental European/Japanese Model:**
- Limit distributions to accumulated profits
- Complex recalculation required to reconcile financial reporting with distribution restrictions
- Ensures capital preservation for creditor protection

### Shareholder Rights and Protections

#### Core Shareholder Rights

**1. Voting Rights (議決権):**
- **Default:** One share = one vote
- **Non-Voting Shares:** Can be created via articles of incorporation
  - Limited voting shares allowed
  - Multiple voting shares NOT allowed (one share cannot have >1 vote)
- **Class Shares:** Different classes can have different rights

**2. Dividend Rights (配当請求権):**
- Right to receive dividends when distributed
- Can have preferred or subordinate dividend rights via class shares
- Default: Shareholder resolution required for dividend payment

**3. Liquidation Distribution Rights:**
- Right to participate in distribution upon company dissolution
- Can have preferred or subordinate rights via class shares

**4. Preemptive Rights:**
- When share acquisition rights issued to existing shareholders (with/without consideration)
- Shareholders entitled to subscribe pro-rata to shareholding
- Right of first refusal typically included in shareholders' agreements
- Exit rights: Drag-along, tag-along rights (contractual, not statutory)

**5. Information Rights:**
- Right to inspect accounting books and records
- Right to receive financial statements
- Right to attend and speak at shareholders' meetings

**6. Derivative Suit Rights:**
- Easily initiated and maintained under Japanese law
- Shareholder can sue directors on behalf of company
- Strong enforcement mechanism

**7. Proposal Rights:**
- Right to propose agenda items for shareholders' meeting
- **Limitation (2019 Amendment):** Maximum 10 proposals per shareholder
  - Prevents abuse from excessive proposals

**8. Enjoin Unlawful Issuance:**
- Right to enjoin share issuance that violates laws/articles
- If issuance likely to cause shareholder disadvantage
- Protection against dilutive issuances for management entrenchment

#### Shareholder Equality Principle

**Legal Requirement (Companies Act):**
- Company must treat shareholders equally based on:
  - Number of shares held
  - Class of shares held
- Cannot provide disproportionate benefits/disadvantages to specific shareholders

**Exception for Private Companies:**
- Non-public companies can include provisions in articles allowing different treatment of shareholders
- Requires explicit authorization in articles of incorporation

#### Shareholder Liability Limitation

**Limited Liability Principle:**
- Shareholders NOT liable for corporate acts or omissions
- Liability limited to amount invested in subscribed shares
- Fundamental principle of corporate law

#### Share Transfer Restrictions

**Transfer-Restricted Shares (譲渡制限株式):**
- **Purpose:** Prevent undesirable third parties from becoming shareholders
- **Mechanism:** Company approval required for share transfer
- **Enforcement:** Without approval:
  - Cannot register transfer in corporate registry
  - Purchaser cannot exercise shareholder rights
- **Private Company:** If ALL shares have transfer restrictions in articles

**Share Classes:**
Companies can issue different classes of shares with varying rights:
1. **Dividend Preference Shares:** Preferred/subordinate dividend rights
2. **Liquidation Preference Shares:** Preferred/subordinate liquidation distribution
3. **Non-Voting Shares:** No or limited voting rights
4. **Transfer-Restricted Shares:** Company approval required for transfer

**Limitations on Share Classes:**
- Cannot issue shares with multiple votes per share
- Must define class rights in articles of incorporation
- Class shares issuance limited to types designated in Companies Act

### Registration and Corporate Seal Requirements

#### Legal Affairs Bureau (法務局) Registration

**Requirement:** All companies must register with Legal Affairs Bureau (Ministry of Justice)
- **Legal Effect:** Company does not exist until registration complete (Companies Act Art. 911)
- **Jurisdiction:** Bureau with jurisdiction over head office location
- **Method:** Written application or online

**Required Registration Matters:**

**For Stock Companies (K.K.):**
- Company purpose (事業目的)
- Trade name (商号)
- Head office and branch office addresses
- Stated capital amount
- Total number of authorized shares
- Details of shares issued
- Director names and addresses
- Representative director name and address
- Corporate governance structure type

**For Limited Liability Companies (G.K.):**
- Company purpose
- Trade name
- Head office and branch office addresses
- Member names and addresses
- Confirmation of limited liability for all members
- Contribution purposes and values

**Change Registration:**
- **Timeline:** Typically within 2 weeks of change
- **Changes Requiring Registration:**
  - Business purpose amendments
  - Location transfers
  - Executive changes
  - Capital increases
  - Organizational changes (mergers, splits)
  - Dissolution
- **Penalty:** Civil fines up to JPY 1,000,000 for failure to register
- **Director Liability:** Directors personally liable for registration failures

#### Corporate Seal (会社印) Requirements

**Legal Requirement:** All companies must have registered company seal

**Types of Corporate Seals:**

1. **Representative Seal (代表印 Daihyo-in):**
   - **REQUIRED:** Must be registered at Legal Affairs Bureau
   - **Authority:** Legally binds the company
   - **Usage:** Official contracts, registration documents, bank account opening
   - **Also Called:** Company seal, corporate seal

2. **Banking Seal (銀行印 Ginko-in):**
   - **Recommended:** Separate seal for banking transactions
   - **Purpose:** Risk management (protect representative seal)
   - **Can Use Representative Seal:** But not recommended

3. **Identification Seal (角印 Kaku-in):**
   - **Optional:** Not registered with government
   - **Usage:** Day-to-day operations, invoices, quotations
   - **Less Important:** Does not have legal binding authority

**Registration Process:**
- Seal registered simultaneously with company establishment registration
- Registration at Legal Affairs Bureau in incorporation jurisdiction
- Obtainable documents after registration:
  - **Certificate of Registered Information (登記事項証明書 Touki-jikou-shomeisho)**
  - **Registered Seal Certificate (印鑑証明書 Inkan-shomeisho)**

**Personal Seal Requirements (For Incorporators/Directors):**
- **Japanese Residents:** Seal certificate (印鑑証明書 inkan-shomeisho) from city hall
  - Must be issued within 3 months
  - Obtained by registering personal seal at municipal office
- **Foreign Nationals/Non-Residents:** Signature attestation
  - Notarized signature from embassy/consulate in Japan, OR
  - Notarized signature from notary public in home country
  - Japanese translation required

**For Foreign Parent Companies:**
- **Alternative Documents Required:**
  - Corporate registration certificate (with Japanese translation)
  - Affidavits on corporate profile (notarized)
  - Signature certificate of parent company CEO (notarized)
  - Reason: Foreign corporations don't have seal impression certifications

#### Articles of Incorporation (定款 Teikan)

**Legal Status:**
- Foundation document defining company structure, purpose, operations
- Required under Companies Act (Art. 27-38)
- Filed with Legal Affairs Bureau
- Carries legal weight and binding authority

**Required Contents:**

**For Stock Companies (K.K.):**
1. Company purpose (目的)
2. Trade name (商号)
3. Head office location (本店所在地)
4. Total number of authorized shares (発行可能株式総数)
5. Initial capital contribution information
6. Incorporator names and addresses

**For Limited Liability Companies (G.K.):**
1. Company purpose
2. Trade name
3. Head office location
4. Member names and addresses
5. Statement that all members are limited liability members
6. Contribution purposes and values/valuation standards

**Optional Provisions:**
- Share transfer restrictions
- Different classes of shares
- Director terms and numbers
- Shareholder meeting quorum and voting requirements
- Board of directors provisions
- Dividend decision authority delegation
- Other governance matters

**Notarization:**
- **K.K.:** MUST be notarized by Japanese notary public
- **G.K.:** No notarization required
- **Cost:** Notary fees apply for K.K. (part of formation costs)

**Signature/Seal Requirements:**
- All incorporators must sign or affix seal
- All directors must sign or affix seal
- Personal seal certificates or signature attestations required

**Amendment Process:**
- Requires extraordinary resolution at shareholders' meeting (2/3 majority)
- Must register amendment with Legal Affairs Bureau
- Changes become effective upon registration

---

## Business Operations Framework

### Business Purpose (事業目的) Requirements

**Legal Framework:**
All companies must register their business purposes in:
1. Articles of Incorporation (定款)
2. Corporate Registry at Legal Affairs Bureau

**Purpose of Business Purpose Registration:**
- Defines scope of company's legal capacity
- Ultra vires acts (beyond business purpose) may be invalid
- Protects shareholders and creditors by limiting company activities
- Required for legal entity status

**Drafting Requirements:**

**Specificity:**
- Must be specific enough to understand the business
- Cannot be overly vague or generic
- Common mistake: Using vague purposes that don't meet legal standards

**Flexibility:**
- Can include multiple business purposes
- Should anticipate future business expansion
- Common to include catch-all clause: "All business incidental to the above"

**Typical Structure:**
```
1. [Specific primary business activity]
2. [Specific secondary business activity]
3. [Additional business activities]
...
N. All business incidental to and related to the foregoing
```

**Examples of Specific Purposes:**
- "Design, development, and sale of computer software"
- "Import, export, and wholesale of food products"
- "Provision of consulting services for corporate management"
- "Operation of restaurants and food service establishments"

**Amendment Process:**
- Requires amendment to articles of incorporation
- Extraordinary shareholder resolution (2/3 majority)
- File change registration with Legal Affairs Bureau within 2 weeks
- Potential penalty for failure to register: Up to JPY 1,000,000 fine

### Fiscal Year and Annual Obligations

**Fiscal Year:**
- **Setting:** Defined in articles of incorporation
- **Common Practice:** March 31 year-end (aligned with Japanese government fiscal year)
- **Flexibility:** Can choose any 12-month period
- **First Year:** Can be less than 12 months

**Annual Obligations Timeline:**

**Within 3 Months After Fiscal Year End:**
1. Prepare financial statements and business report
2. Statutory auditor review (if applicable)
3. Board of directors approval
4. Hold annual general shareholders' meeting
5. File annual securities report (listed companies)

**Within 2 Weeks of Changes:**
- Register any changes with Legal Affairs Bureau
- Includes: Director changes, capital changes, address changes

**Continuous:**
- Maintain corporate books and records
- Keep minutes of shareholders' and board meetings
- Update shareholder registry
- Maintain accounting books

### Corporate Books and Records

**Required Corporate Records:**

1. **Shareholder Registry (株主名簿):**
   - Names and addresses of all shareholders
   - Number and class of shares held
   - Date of acquisition
   - Must be maintained at head office

2. **Articles of Incorporation:**
   - Original certified copy
   - All amendments with effective dates

3. **Meeting Minutes:**
   - **Shareholders' Meetings:** All resolutions and discussions
   - **Board Meetings:** All decisions and deliberations
   - **Committee Meetings:** For companies with committees
   - Must be signed/sealed by attendees

4. **Accounting Books:**
   - General ledger
   - Journals
   - Supporting documents
   - Retention: 10 years after fiscal year end

5. **Financial Statements:**
   - Annual financial statements
   - Audit reports
   - Business reports
   - Retention: 10 years

**Inspection Rights:**
- Shareholders can request inspection during business hours
- Must show reasonable purpose
- Company can refuse if purpose deemed improper

### Legal Representative Requirements

**Representative Director (代表取締役):**
- **Required:** All K.K. companies must have representative director
- **Authority:** Full authority to represent and bind company
- **Registration:** Must be registered at Legal Affairs Bureau
- **Seal:** Representative seal registered with personal information
- **Residency:** No legal requirement for Japanese residency (but practical considerations apply)

**Foreign Nationals as Representative Director:**
- **Legally Permitted:** No prohibition against foreign nationals
- **Practical Requirements:**
  - Appropriate visa status (Business Manager visa typically required)
  - Registered address in Japan (often required by banks)
  - Japanese seal certificate or notarized signature
- **Visa Considerations:**
  - Business Manager visa requires JPY 30,000,000 capital investment
  - Alternative: Japanese national or permanent resident as representative director

**Multiple Representative Directors:**
- **Joint Representatives:** Can require multiple signatures
- **Several Representatives:** Each can act independently
- Specified in articles of incorporation and registered

---

## Recent Amendments (2019-2022)

### 2019 Companies Act Amendment

**Promulgated:** December 4, 2019
**Primary Effective Date:** March 1, 2021
**Background:** Response to corporate governance concerns from domestic and foreign investors

#### 1. Electronic Provision of Shareholder Meeting Materials

**Old System:**
- Consent of each shareholder required to provide materials electronically
- Default was physical mailing

**New System (Effective September 1, 2022):**
- **Listed Companies:** MUST make materials available on internet
- **Timeline:** At least 3 weeks before shareholder meeting
- **Process:** Amendment to articles of incorporation required
- **No Individual Consent:** No longer need each shareholder's consent
- **Paper Option:** Must still send abbreviated documents unless shareholder opts for full electronic

**Implementation:**
- System modified by Japan Securities Depository Center
- Significantly reduces printing and mailing costs
- Aligns with global best practices

#### 2. Virtual Shareholder Meetings

**Companies Act Limitation:**
- General shareholders' meetings CANNOT be held solely through virtual means
- Physical meeting still required
- Hybrid allowed: Physical meeting + remote participation option

**Industrial Competitiveness Enhancement Act Amendment (June 2021):**
- **New Capability:** Virtual-only shareholder meetings now possible
- **Requirements:**
  1. Minister of Economy, Trade and Industry confirmation
  2. Minister of Justice confirmation
  3. Satisfy conditions in ministry ordinances
  4. Amend articles of incorporation to allow "shareholder meeting without designated location"
- **Effective:** June 16, 2021
- **Usage:** Limited adoption; most companies still prefer physical/hybrid

#### 3. Limitation on Shareholder Proposals

**Problem:** Some shareholders abusing proposal rights with excessive proposals

**Solution (2019 Amendment):**
- **Limit:** Maximum 10 proposals per shareholder at single meeting
- **Purpose:** Prevent abusive practices
- **Balance:** Maintains shareholder rights while preventing disruption

#### 4. Outside Director Requirement for Listed Companies

**New Requirement:**
- ALL listed companies must appoint at least 1 outside director
- Previously only encouraged, now mandatory
- Part of broader corporate governance reforms

#### 5. D&O Liability Insurance Provisions

**New Framework:**
- Clarified procedures for D&O insurance contracts
- **Board Approval Required:** When deciding/changing D&O insurance
  - Board of directors resolution, OR
  - Shareholders' meeting resolution (if no board)
- **Special Rule:** Article 356 conflict-of-interest rules do NOT apply
  - Separate framework under Article 430-3 established
  - Avoids duplication of conflict rules
- **Disclosure Required:** Terms of insurance to be disclosed

**Purpose:**
- Address potential conflicts between company and insured directors
- Ensure proper oversight of insurance that could affect director behavior
- Protect propriety of director duty execution

#### 6. Director Compensation Disclosure

**Enhanced Requirements:**
- Greater transparency in director compensation
- Alignment with international standards
- Particularly for listed companies

### Corporate Governance Code Developments

**Not Part of Companies Act, but Related:**

**Tokyo Stock Exchange Corporate Governance Code:**
- Updated multiple times (2015, 2018, 2021)
- Soft law (comply or explain principle)

**Key Provisions:**
- **Prime Market:** 1/3+ independent outside directors
- **Other Markets:** 2+ independent outside directors
- **Board Diversity:** Encouraged
- **Disclosure:** Corporate governance structure and policies
- **Shareholder Engagement:** Encouraged dialogue

**2021 Updates:**
- Sustainability disclosure encouraged
- Climate-related risk disclosure (TCFD framework)
- Diversity targets for boards
- Skills matrix disclosure

### Tokyo Stock Exchange Requirements (2024-2025)

**English Disclosure Requirement (April 2025):**
- **Applies To:** Prime Market listed companies
- **Requirement:** All disclosures in BOTH Japanese and English
- **Purpose:** Enhance accessibility for foreign investors
- **Significance:** Major change for international transparency

### Financial Instruments and Exchange Act Amendments

**ESG and Sustainability Disclosure (2022-2023):**
- Mandatory disclosure of sustainability efforts
- ESG considerations must be reported
- Alignment with global reporting standards

**Quarterly Reporting:**
- Continues to be required
- 45-day deadline after quarter end

---

## Domain Model Mapping

### Recommended Domain-Driven Design Structure

Based on the Japanese Companies Act research, here's a suggested domain model organization for implementing company-as-code:

#### 1. Core Domain - Company Formation and Structure

**Aggregates:**

**Company Aggregate Root:**
```
Company
├── CompanyId (Value Object)
├── CompanyType (Enum: KabushikiKaisha, GodoKaisha, GomeiKaisha, GoshiKaisha)
├── CompanyName (Value Object)
├── HeadOffice (Entity: Address)
├── BranchOffices (Collection<Entity: Address>)
├── Articles OfIncorporation (Entity)
├── BusinessPurposes (Collection<Value Object>)
├── EstablishmentDate (Value Object: Date)
├── FiscalYearEnd (Value Object: MonthDay)
├── StatedCapital (Value Object: Money)
├── CorporateSeal (Entity)
├── RegistrationStatus (Value Object)
└── GovernanceModel (Enum: StatutoryAuditor, AuditCommittee, ThreeCommittees)
```

**Capital Structure Aggregate:**
```
CapitalStructure
├── StatedCapital (Value Object: Money)
├── TotalAuthorizedShares (Value Object: Quantity)
├── IssuedShares (Collection<Entity: ShareIssuance>)
├── ShareClasses (Collection<Entity: ShareClass>)
├── Treasury Stock (Value Object: Quantity)
├── CapitalReserve (Value Object: Money)
├── ProfitReserve (Value Object: Money)
└── DistributableAmount (Calculated Value Object: Money)
```

#### 2. Governance Domain

**Aggregates:**

**Board of Directors Aggregate:**
```
BoardOfDirectors
├── Directors (Collection<Entity: Director>)
├── RepresentativeDirectors (Collection<Entity: RepresentativeDirector>)
├── OutsideDirectors (Collection<Entity: OutsideDirector>)
├── BoardMeetings (Collection<Entity: BoardMeeting>)
├── Resolutions (Collection<Entity: BoardResolution>)
├── MinimumDirectors (Value Object: int - business rule based on governance model)
└── GovernanceModel (Enum)
```

**Director Entity:**
```
Director
├── DirectorId (Value Object)
├── PersonalInfo (Value Object: Person)
├── AppointmentDate (Value Object: Date)
├── TermEndDate (Value Object: Date)
├── TermLength (Value Object: Duration - max 2 years for directors)
├── DirectorType (Enum: Executive, NonExecutive, Outside)
├── FiduciaryDuties (Collection<Value Object: Duty>)
├── CompensationAgreement (Entity)
└── LiabilityLimitation (Entity - if applicable)
```

**Statutory Auditor Aggregate (for Statutory Auditor model):**
```
StatutoryAuditorBoard
├── Auditors (Collection<Entity: StatutoryAuditor>)
├── OutsideAuditors (Collection<Entity: StatutoryAuditor>)
├── AuditorMeetings (Collection<Entity: AuditorMeeting>)
├── AuditReports (Collection<Entity: AuditReport>)
└── MinimumAuditors (Value Object: int)
```

**Committee Aggregate (for Committee models):**
```
Committee
├── CommitteeType (Enum: Audit, Nominating, Compensation, AuditAndSupervisory)
├── Members (Collection<Entity: Director>)
├── OutsideDirectorMajority (Business Rule: bool)
├── Meetings (Collection<Entity: CommitteeMeeting>)
├── Resolutions (Collection<Entity: CommitteeResolution>)
└── MinimumMembers (Value Object: int - must be 3+)
```

**Executive Officers Aggregate (for Three-Committee model):**
```
ExecutiveOfficers
├── Officers (Collection<Entity: ExecutiveOfficer>)
├── RepresentativeExecutiveOfficers (Collection<Entity: RepresentativeExecutiveOfficer>)
├── AppointingBoardResolution (Value Object: ResolutionId)
└── TermLength (Value Object: Duration - 1 year)
```

#### 3. Shareholder Domain

**Shareholder Registry Aggregate:**
```
ShareholderRegistry
├── Shareholders (Collection<Entity: Shareholder>)
├── ShareTransferRestrictions (Value Object: bool)
├── IsPublicCompany (Calculated from transfer restrictions)
└── UpdateHistory (Collection<Event: RegistryUpdate>)
```

**Shareholder Entity:**
```
Shareholder
├── ShareholderId (Value Object)
├── PersonOrCorporation (Value Object: LegalEntity)
├── SharesHeld (Collection<Value Object: ShareHolding>)
├── AcquisitionDate (Value Object: Date)
├── VotingRights (Calculated based on shares)
└── DividendRights (Calculated based on shares)
```

**Shareholders Meeting Aggregate:**
```
ShareholdersMeeting
├── MeetingId (Value Object)
├── MeetingType (Enum: Annual, Extraordinary)
├── MeetingDate (Value Object: DateTime)
├── Agenda (Collection<Entity: AgendaItem>)
├── Attendees (Collection<Value Object: Attendance>)
├── Resolutions (Collection<Entity: ShareholderResolution>)
├── QuorumRequirements (Value Object - varies by resolution type)
├── VotingRequirements (Value Object - varies by resolution type)
├── MaterialsProvidedDate (Value Object: Date - must be 3 weeks before for listed)
└── Minutes (Entity: MeetingMinutes)
```

**ShareholderResolution Entity:**
```
ShareholderResolution
├── ResolutionId (Value Object)
├── ResolutionType (Enum: Ordinary, Extraordinary, SpecialMajority)
├── Description (Value Object: Text)
├── VotesFor (Value Object: Quantity)
├── VotesAgainst (Value Object: Quantity)
├── Abstentions (Value Object: Quantity)
├── QuorumMet (bool - calculated)
├── VoteThresholdMet (bool - calculated)
├── Passed (bool)
└── EffectiveDate (Value Object: Date)
```

#### 4. Financial and Compliance Domain

**Financial Statements Aggregate:**
```
FinancialStatements
├── FiscalYear (Value Object: Year)
├── BalanceSheet (Entity)
├── ProfitAndLossStatement (Entity)
├── StatementOfChangesInEquity (Entity)
├── NotesToFinancialStatements (Entity)
├── PreparationDate (Value Object: Date)
├── AuditReport (Entity - if required)
├── BoardApprovalDate (Value Object: Date)
├── ShareholderApprovalDate (Value Object: Date)
└── AccountingStandard (Enum: JGAAP, IFRS, USGAAP)
```

**Dividend Distribution Aggregate:**
```
DividendDistribution
├── DistributionId (Value Object)
├── DeclarationDate (Value Object: Date)
├── PaymentDate (Value Object: Date)
├── DividendPerShare (Value Object: Money)
├── TotalDistribution (Calculated Value Object: Money)
├── DistributableAmount (Value Object: Money - from capital structure)
├── ReserveRequirement (Calculated Value Object: Money)
├── NetAssetCheck (Business Rule: bool - must be >= 3,000,000 JPY)
├── AuthorizingResolution (Value Object: ResolutionId)
└── PaymentRecords (Collection<Entity: DividendPayment>)
```

**Annual Compliance Aggregate:**
```
AnnualCompliance
├── FiscalYear (Value Object)
├── FinancialStatements (Reference to Aggregate)
├── BusinessReport (Entity)
├── AnnualShareholdersMeeting (Reference to Entity)
├── AnnualSecuritiesReport (Entity - if listed)
├── QuarterlyReports (Collection<Entity> - if listed)
├── InternalControlReport (Entity - if listed)
├── CorporateGovernanceReport (Entity - if listed)
├── FilingDeadlines (Collection<Value Object: Deadline>)
└── ComplianceStatus (Enum: InProgress, Complete, Overdue)
```

#### 5. Registration Domain

**Corporate Registration Aggregate:**
```
CorporateRegistration
├── RegistrationNumber (Value Object)
├── LegalAffairsBureau (Value Object: Bureau)
├── InitialRegistrationDate (Value Object: Date)
├── RegisteredMatters (Collection<Entity: RegisteredMatter>)
├── ChangeHistory (Collection<Event: RegistrationChange>)
├── CorporateSeal (Entity)
└── RegistrationCertificates (Collection<Entity: Certificate>)
```

**RegisteredMatter Entity:**
```
RegisteredMatter
├── MatterType (Enum: Purpose, TradeName, Address, Capital, Directors, etc.)
├── CurrentValue (Value Object: varies by type)
├── EffectiveDate (Value Object: Date)
├── RegistrationDate (Value Object: Date)
└── ChangeDeadline (Calculated: usually 2 weeks from effective date)
```

#### 6. Domain Events (for Event Sourcing)

**Company Formation Events:**
- `CompanyIncorporated`
- `ArticlesOfIncorporationAmended`
- `BusinessPurposeAdded`
- `BusinessPurposeRemoved`

**Governance Events:**
- `DirectorAppointed`
- `DirectorRemoved`
- `RepresentativeDirectorDesignated`
- `BoardMeetingHeld`
- `BoardResolutionPassed`
- `CommitteeEstablished`
- `CommitteeMemberAppointed`

**Shareholder Events:**
- `SharesIssued`
- `SharesTransferred`
- `ShareholderAdded`
- `ShareholderRemoved`
- `ShareholdersMeetingHeld`
- `ShareholderResolutionPassed`
- `DividendDeclared`
- `DividendPaid`

**Compliance Events:**
- `FinancialStatementsApproved`
- `AnnualReportFiled`
- `RegistrationUpdated`
- `CapitalIncreased`

#### 7. Domain Services

**Validation Services:**
- `CompanyFormationValidator` - Validates formation requirements based on company type
- `GovernanceStructureValidator` - Ensures governance structure meets legal requirements
- `CapitalMaintenanceValidator` - Validates dividend distributions against capital maintenance rules
- `ShareholderMeetingValidator` - Validates quorum and voting requirements
- `RegistrationComplianceValidator` - Ensures registration changes filed within deadlines

**Calculation Services:**
- `DistributableAmountCalculator` - Calculates amount available for dividend distribution
- `ReserveRequirementCalculator` - Calculates required reserves upon dividend payment
- `VotingRightsCalculator` - Calculates voting rights based on share ownership
- `QuorumCalculator` - Calculates whether meeting quorum requirements met

**Business Rule Services:**
- `DirectorTermService` - Manages director term expiration and renewal
- `FiscalYearService` - Manages fiscal year deadlines and obligations
- `ShareTransferApprovalService` - Manages share transfer approval process for restricted shares

#### 8. Bounded Context Relationships

**Core Contexts:**
1. **Company Formation Context** - Company establishment and structure
2. **Governance Context** - Directors, officers, auditors, committees
3. **Shareholder Context** - Shareholders, meetings, resolutions
4. **Capital Context** - Shares, capital, dividends, reserves
5. **Compliance Context** - Financial reporting, annual obligations
6. **Registration Context** - Legal Affairs Bureau registration

**Context Map:**
```
Company Formation Context
  ├─ Shared Kernel with → Governance Context (company governance model)
  ├─ Conformist → Registration Context (registration requirements)
  └─ Customer-Supplier → Capital Context (capital structure requirements)

Governance Context
  ├─ Shared Kernel with → Company Formation Context
  ├─ Partnership with → Shareholder Context (director appointments)
  └─ Customer-Supplier → Compliance Context (governance reporting)

Shareholder Context
  ├─ Partnership with → Governance Context
  ├─ Customer-Supplier → Capital Context (share ownership)
  └─ Conformist → Compliance Context (shareholder reporting)

Capital Context
  ├─ Published Language → Compliance Context (financial statements)
  └─ Anti-Corruption Layer → Shareholder Context (dividend rights)

Compliance Context
  ├─ Conformist → Registration Context (filing requirements)
  └─ Open Host Service → All Contexts (compliance status)

Registration Context
  └─ Published Language → All Contexts (registered information)
```

#### 9. Invariants and Business Rules

**Company Formation Invariants:**
- K.K. with board must have minimum 3 directors
- K.K. with board must have statutory auditor(s) (unless committee model)
- Company must have at least one representative director
- Business purposes must be specific and lawful
- Articles of incorporation must contain required matters

**Governance Invariants:**
- Director terms cannot exceed 2 years (1 year for audit committee members)
- Statutory auditor terms are 4 years
- Listed companies must have at least 1 outside director
- Three-committee companies must have majority outside directors on each committee
- Audit committee must have at least 3 members with majority outside directors

**Capital Maintenance Invariants:**
- Cannot pay dividends if net assets < JPY 3,000,000
- Dividends cannot exceed distributable amount
- Must set aside reserves (10% of distribution or until reserves = 25% of capital)
- Treasury stock reduces distributable amount

**Shareholder Meeting Invariants:**
- Ordinary resolution: majority of votes present (quorum varies)
- Extraordinary resolution: 2/3 of votes present with quorum
- Director election minimum quorum: 1/3 (cannot be eliminated)
- Material provision deadline: 3 weeks for listed companies (electronic)

**Compliance Invariants:**
- Annual shareholders' meeting within 3 months of fiscal year end
- Registration changes within 2 weeks of effective date
- Large companies must have accounting audit
- Listed companies must file annual securities report within 3 months

---

## Sources

### Legal Framework and Company Types

- [Japanese Law Translation - Companies Act (Japanese/English)](https://www.japaneselawtranslation.go.jp/en/laws/view/2035)
- [Japanese Law Translation - Companies Act (English)](https://www.japaneselawtranslation.go.jp/en/laws/view/3206/en)
- [weConnect - Laws & Regulations for Japan Entity Incorporation](https://weconnect.co/japan/laws-regulations-on-japan-entity-setup/)
- [ICLG - Corporate Governance Laws and Regulations Report 2025 Japan](https://iclg.com/practice-areas/corporate-governance-laws-and-regulations/japan)
- [Japan Compliance - Key Differences in Corporate Structures under Japanese Company Law for Foreign Investors](https://japancompliance.com/what-are-the-key-differences-in-corporate-structures-under-japanese-company-law-for-foreign-investors/)

### Formation Requirements and Costs

- [SmartStart Japan - Ultimate Guide to Godo Kaisha in Japan](https://smartstartjapan.com/ultimate-guide-to-godo-kaisha-in-japan/)
- [Export to Japan - Godo Kaisha GK (Limited Liability) company in Japan](https://exporttojapan.co.uk/guide/setting-up-in-japan/godo-gaisha/)
- [Venture Japan - Setting up with a GK godo kaisha](https://www.venturejapan.com/doing-business-in-japan/how-to-start-a-company-in-japan/setting-up-with-gk-godo-kaisha/)
- [OnDemand International - Godo Kaisha in Japan: Complete Guide for 2025](https://ondemandint.com/resources/godo-kaisha-in-japan/)
- [Japan Compliance - Establishing a Company in Japan: Understanding the Registration and License Tax](https://japancompliance.com/establishing-a-company-kabushiki-kaisha-or-godo-kaisha-in-japan-understanding-the-registration-and-license-tax/)
- [E-Housing - Setting Up a Company in Japan: KK vs GK](https://e-housing.jp/post/setting-up-a-company-in-japan-kk-vs-gk-kabushiki-and-godo-kaisha)

### Corporate Governance

- [Nagashima Ohno & Tsunematsu - Corporate Governance Law and Practice](https://www.noandt.com/wp-content/uploads/2019/06/cp_gpg_CorporateGovernance_2019.pdf)
- [Chambers Global Practice Guides - Corporate Governance 2024](https://practiceguides.chambers.com/practice-guides/comparison/1049/13536/21458-21459-21460-21461-21462-21463-21464)
- [Chambers - Corporate Governance 2025 - Japan](https://practiceguides.chambers.com/practice-guides/corporate-governance-2025/japan)
- [Loeb & Loeb - Corporate Governance Exemptions Available to Japanese Companies Seeking to List on Nasdaq](https://www.loeb.com/en/insights/publications/2024/08/corporate-governance-exemptions-available-to-japanese-companies-seeking-to-list-on-nasdaq)
- [Lexology - Japan Corporate Governance Structure](https://www.lexology.com/library/detail.aspx?g=0057a1d7-bd41-4037-a7d3-03c7aae2593f)

### 2019-2021 Amendments

- [Waseda University - Amendments to the Companies Act, 2019](https://www.waseda.jp/folaw/icl/news/2021/03/26/7015/)
- [Jones Day - Japan Legal Update Vol. 50 | November–December 2019](https://www.jonesday.com/en/insights/2020/01/japan-legal-update-vol-50)
- [Legal500 - Shareholder Meeting Digitalization in Japan – Legal Developments](https://www.legal500.com/developments/thought-leadership/shareholder-meeting-digitalization-in-japan/)
- [Monolith Law Office - Electronic Provision System for Shareholders' Meeting Materials](https://monolith.law/en/general-corporate/company-law-revision2022)
- [Chambers - Shareholders' Rights & Shareholder Activism 2024](https://practiceguides.chambers.com/practice-guides/comparison/978/14377/22418-22419-22420-22421-22422-22423-22424-22425-22426-22427-22428)

### Financial Disclosure and Reporting

- [JICPA - Corporate Disclosure in Japan Overview](https://www.hp.jicpa.or.jp/english/about/publications/pdf/PUBLICATION-Overview2010.pdf)
- [Financial Services Agency - FAQ on Financial Instruments and Exchange Act](https://www.fsa.go.jp/en/laws_regulations/faq_on_fiea/section03.html)
- [Generis Online - Annual Filing and Reporting Obligations for Companies in Japan](https://generisonline.com/annual-filing-and-reporting-obligations-for-companies-in-japan/)
- [Tokyo Stock Exchange - Guidebook for the Timely Disclosure of Corporate Information](https://www.jpx.co.jp/english/equities/listing/disclosure/guidebook/dh3otn0000000xbv-att/Guidebook.pdf)
- [Waseda University Library - Find Annual Securities Reports](https://waseda-jp.libguides.com/research-navi/find_securities/en)

### Shareholder Rights and Capital Maintenance

- [De Gruyter - Shareholders' Equity and Dividend Regulation in Japan](https://www.degruyterbrill.com/document/doi/10.1515/ael-2024-0040/html?lang=en)
- [Japan Law Guide - Shareholder Rights](https://japanlawguide.com/shareholder-rights/)
- [University of Michigan - Legally "Strong" Shareholders of Japan](https://repository.law.umich.edu/mbelr/vol3/iss2/3/)
- [Legal500 - Japan: Doing Business In](https://www.legal500.com/guides/chapter/japan-doing-business-in/)
- [Monolith Law Office - Issuance and Contents of Preferred Shares in Venture Investment Contracts](https://monolith.law/en/general-corporate/issuance-of-class-shares)

### Articles of Incorporation and Corporate Seals

- [Juridique - Required Documents for Incorporation in Japan](https://www.juridique.jp/business/incorporation_documents.php)
- [Ministry of Justice - Procedures for Establishment of Limited Liability Companies](https://www.moj.go.jp/EN/MINJI/m_minji06_00003.html)
- [Hanko-Seal - How to create "Articles of incorporation" = "Teikan"](https://hanko-seal.com/archives/7063)
- [JETRO - Procedures for registering establishment](https://www.jetro.go.jp/en/invest/setting_up/section1/page3.html)
- [weConnect - Articles of Incorporation in Japan: Requirements & Timelines](https://weconnect.co/japan/articles-of-incorporation-requirements/)
- [Ministry of Justice - Procedures for Establishment of Stock Companies](https://www.moj.go.jp/EN/MINJI/m_minji06_00001.html)

### Share Issuance and Transfer

- [Monolith Law Office - Issuance and Contents of Preferred Shares](https://monolith.law/en/general-corporate/issuance-of-class-shares)
- [TerrаLex - Basics of Japanese Corporate Law in Equity Acquisition](https://www.terralex.org/publications/basics-of-japanese-corporate-law-in-equity-acquisition)
- [ICLG - Mergers & Acquisitions Laws and Regulations Report 2025 Japan](https://iclg.com/practice-areas/mergers-and-acquisitions-laws-and-regulations/japan)
- [Chambers - Equity Finance 2024 - Japan](https://practiceguides.chambers.com/practice-guides/equity-finance-2024/japan/trends-and-developments)

### Director Liability and D&O Insurance

- [Monolith Law Office - Japanese Directors and Officers Liability Insurance Contract](https://monolith.law/en/general-corporate/directors-liability-insurance-contract)
- [Japan Compliance - Parent Company Director Liability in Japan](https://japancompliance.com/parent-company-director-liability-in-japan-oversight-of-subsidiary-compliance-failures/)
- [Lexology - At a glance: responsibilities of company boards in Japan](https://www.lexology.com/library/detail.aspx?g=eea0cb86-41ab-4de1-841c-70c8af89e159)
- [IBA - Company Director Checklist – Japan](https://www.ibanet.org/document?id=Directors-Duties-Japan-22)

### Representative Directors and Executive Officers

- [Wikipedia - Representative director (Japan)](https://en.wikipedia.org/wiki/Representative_director_(Japan))
- [Vcheck Global - Japanese Corporate Titles: A Due Diligence Guide](https://vcheckglobal.com/blog/whos-in-charge-around-here-understanding-japans-corporate-hierarchy/)
- [Japanese Law Translation - Companies Act](https://www.japaneselawtranslation.go.jp/en/laws/view/3206)
- [Japan Dev - Job Titles and Company Positions in Japan: A Complete Guide](https://japan-dev.com/blog/company-positions-and-job-titles-in-japanese)

### Corporate Governance Structures

- [IFLR1000 - New corporate governance structure: Company with audit and supervisory committee](https://www.iflr1000.com/NewsAndAnalysis/new-corporate-governance-structure-company-with-audit-and-supervisory-committee/Index/5997)
- [Monolith Law Office - Explanation of Companies with Audit and Supervisory Committees](https://monolith.law/en/general-corporate/audit-committee-company-japan)
- [Lexology - A general introduction to Corporate Governance in Japan](https://www.lexology.com/library/detail.aspx?g=cc57e09c-8675-4e53-af0d-9689995bbbfd)
- [The Law Reviews - Japan - The Corporate Governance Review - Edition 9](https://thelawreviews.co.uk/edition/the-corporate-governance-review-edition-9/1189455/japan)

### Business Purpose and Registration

- [Japanese Law Translation - Commercial Registration Act (English)](https://www.japaneselawtranslation.go.jp/en/laws/view/1863/en)
- [JETRO - Laws & Regulations on Setting Up Business in Japan](https://www.jetro.go.jp/ext_images/_Invest/pdf/laws/laws_regulations_201612_en.pdf)
- [i-socia Advisors - TYPES of BUSINESS OPERATION](https://eng.daikou-office.com/reference/businesstype/)

### Shareholders' Meetings

- [DLA Piper - Quorum requirements for shareholder and board meetings in Japan](https://www.dlapiperintelligence.com/goingglobal/corporate/index.html?t=32-quorum-requirements&c=JP)
- [Chambers - Shareholders' Rights & Shareholder Activism 2023 - Japan](https://practiceguides.chambers.com/practice-guides/shareholders-rights-shareholder-activism-2023/japan)
- [Chambers - Shareholders' Rights & Shareholder Activism 2025 - Japan](https://practiceguides.chambers.com/practice-guides/shareholders-rights-shareholder-activism-2025/japan)
- [Iwaidalaw - Shareholders' Meetings in Japan in the situation on COVID](https://www.iwaidalaw.com/en/pdf/200515.pdf)
- [Niizawa Law - Japanese Corporate Law 2 (Board of Directors, Shareholders' Meeting, Annual Reporting Obligations)](https://www.niizawa-law.com/articles/japanese-corporate-law-2-board-of-directors-shareholders-meeting-annual-reporting-obligations/)

---

## Document Information

**Compiled by:** Research Agent
**Date:** 2025-11-28
**Total Sources:** 80+ authoritative sources
**Primary Legislation:** Japanese Companies Act (Act No. 86 of 2005)
**Purpose:** Comprehensive research for Domain-Driven Design implementation of Japanese corporate structures

**Note:** This research document provides detailed legal information for modeling purposes. For actual legal compliance, consult qualified Japanese legal counsel. Laws and regulations are subject to change.
