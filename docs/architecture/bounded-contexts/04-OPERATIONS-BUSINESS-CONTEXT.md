# Operations & Business Context

## Context Overview

**Domain**: Business operations, customer management, sales, procurement
**Type**: Generic/Supporting Domain
**Strategic Pattern**: Customer-Supplier (consumes from Legal and Financial contexts)

## Ubiquitous Language

### Japanese Terms
- **Torihikisaki (取引先)**: Business partner (customer or vendor)
- **Tokui Saki (得意先)**: Customer
- **Shiire Saki (仕入先)**: Supplier/vendor
- **Seikyuusho (請求書)**: Invoice
- **Mitsumorisho (見積書)**: Quotation
- **Chuumonsha (注文書)**: Purchase order
- **Nouhinsha (納品書)**: Delivery note
- **Ryoshuusho (領収書)**: Receipt
- **Nouhin (納品)**: Delivery
- **Juchuu (受注)**: Order received
- **Hacchuu (発注)**: Order placed
- **Zaiko (在庫)**: Inventory
- **Shouhin (商品)**: Product/merchandise
- **Keiyaku (契約)**: Contract
- **Torihiki Jouken (取引条件)**: Trading terms

## Aggregate Roots

### 1. Customer Aggregate

**Aggregate Root**: `Customer`

**Invariants**:
- Customer code must be unique
- At least one contact method required
- Credit limit must be non-negative
- Payment terms must be defined

**Entities**:
```haskell
data Customer = Customer
  { customerId :: CustomerId
  , companyId :: CompanyId
  , customerCode :: CustomerCode
  , customerInfo :: CustomerInfo
  , customerType :: CustomerType
  , creditLimit :: Maybe Money
  , paymentTerms :: PaymentTerms
  , billingAddress :: Address
  , shippingAddresses :: [ShippingAddress]
  , contacts :: NonEmpty ContactPerson
  , status :: CustomerStatus
  , statistics :: CustomerStatistics
  }

data CustomerInfo
  = CorporateCustomer
      { corporateName :: Text
      , corporateNameKana :: Text
      , corporateNumber :: Maybe CorporateNumber
      , representative :: Text
      , industry :: Industry
      }
  | IndividualCustomer
      { fullName :: PersonName
      , fullNameKana :: PersonNameKana
      }

data CustomerType
  = RetailCustomer
  | WholesaleCustomer
  | CorporateAccount
  | GovernmentEntity
  | Individual

data CustomerStatus
  = Active
  | OnHold HoldReason
  | Suspended SuspensionReason
  | Inactive InactiveSince

data CustomerStatistics = CustomerStatistics
  { totalOrders :: Int
  , totalRevenue :: Money
  , averageOrderValue :: Money
  , lastOrderDate :: Maybe Date
  , outstandingBalance :: Money
  , creditUsed :: Money
  }
```

**Commands**:
- `RegisterCustomer`
- `UpdateCustomerInfo`
- `SetCreditLimit`
- `SuspendCustomer`
- `ReactivateCustomer`
- `AddShippingAddress`

**Domain Events**:
- `CustomerRegistered`
- `CustomerInfoUpdated`
- `CreditLimitSet`
- `CustomerSuspended`
- `CustomerReactivated`
- `ShippingAddressAdded`

### 2. Sales Order Aggregate

**Aggregate Root**: `SalesOrder`

**Invariants**:
- Order must have at least one line item
- Total must match sum of line items
- Customer must be active
- Inventory must be reserved upon confirmation

**Entities**:
```haskell
data SalesOrder = SalesOrder
  { orderId :: OrderId
  , companyId :: CompanyId
  , orderNumber :: OrderNumber
  , customerId :: CustomerId
  , orderDate :: Date
  , requestedDeliveryDate :: Maybe Date
  , lineItems :: NonEmpty OrderLineItem
  , subtotal :: Money
  , consumptionTax :: Money
  , shippingFee :: Maybe Money
  , total :: Money
  , shippingAddress :: Address
  , orderStatus :: OrderStatus
  , paymentStatus :: PaymentStatus
  , fulfillmentStatus :: FulfillmentStatus
  }

data OrderLineItem = OrderLineItem
  { lineItemId :: LineItemId
  , productId :: ProductId
  , productCode :: ProductCode
  , description :: Text
  , quantity :: Quantity
  , unitPrice :: Money
  , discountRate :: Maybe Percentage
  , lineTotal :: Money
  , taxCategory :: TaxCategory
  , inventoryReservation :: Maybe ReservationId
  }

data OrderStatus
  = Draft
  | Quoted QuotationInfo
  | Confirmed ConfirmationDate
  | InProduction ProductionInfo
  | ReadyToShip
  | Shipped ShipmentInfo
  | Delivered DeliveryConfirmation
  | Completed
  | Cancelled CancellationReason

data FulfillmentStatus
  = Pending
  | PartiallyFulfilled
      { fulfilled :: Quantity
      , remaining :: Quantity
      }
  | FullyFulfilled
  | BackOrdered BackOrderInfo

data PaymentStatus
  = Unpaid
  | PartiallyPaid Money
  | FullyPaid PaymentDate
  | Overdue Days
```

**Commands**:
- `CreateQuotation`
- `ConvertQuotationToOrder`
- `ConfirmOrder`
- `ModifyOrder`
- `CancelOrder`
- `ShipOrder`
- `ConfirmDelivery`

**Domain Events**:
- `QuotationCreated`
- `OrderConfirmed`
- `OrderModified`
- `OrderCancelled`
- `OrderShipped`
- `DeliveryConfirmed`

### 3. Product Catalog Aggregate

**Aggregate Root**: `ProductCatalog`

**Invariants**:
- Product codes must be unique
- Prices must be positive
- Inventory tracking method must be consistent
- Tax categories must be valid

**Entities**:
```haskell
data ProductCatalog = ProductCatalog
  { catalogId :: CatalogId
  , companyId :: CompanyId
  , products :: [Product]
  , categories :: [ProductCategory]
  , priceLists :: [PriceList]
  }

data Product = Product
  { productId :: ProductId
  , productCode :: ProductCode
  , productName :: Text
  , productNameKana :: Text
  , category :: ProductCategoryId
  , description :: Text
  , specifications :: Map Text Text
  , unitOfMeasure :: UnitOfMeasure
  , standardPrice :: Money
  , costPrice :: Maybe Money
  , taxCategory :: TaxCategory
  , inventoryTracking :: InventoryTrackingMethod
  , status :: ProductStatus
  , supplier :: Maybe SupplierId
  }

data ProductCategory = ProductCategory
  { categoryId :: ProductCategoryId
  , categoryName :: Text
  , parentCategory :: Maybe ProductCategoryId
  , sortOrder :: Int
  }

data InventoryTrackingMethod
  = NotTracked
  | Tracked InventoryMethod

data InventoryMethod
  = FIFO    -- First In First Out
  | LIFO    -- Last In First Out
  | Average -- Moving average
  | Specific -- Specific identification

data ProductStatus
  = Active
  | Discontinued DiscontinuedDate
  | OutOfStock
  | Seasonal SeasonalInfo
```

**Commands**:
- `AddProduct`
- `UpdateProduct`
- `DiscontinueProduct`
- `SetProductPrice`
- `CreateProductCategory`

**Domain Events**:
- `ProductAdded`
- `ProductUpdated`
- `ProductDiscontinued`
- `ProductPriceSet`
- `ProductCategoryCreated`

### 4. Vendor Aggregate

**Aggregate Root**: `Vendor`

**Invariants**:
- Vendor code must be unique
- Payment terms must be defined
- At least one contact required

**Entities**:
```haskell
data Vendor = Vendor
  { vendorId :: VendorId
  , companyId :: CompanyId
  , vendorCode :: VendorCode
  , vendorInfo :: VendorInfo
  , vendorType :: VendorType
  , paymentTerms :: PaymentTerms
  , bankAccount :: Maybe BankAccountInfo
  , contacts :: NonEmpty ContactPerson
  , status :: VendorStatus
  , statistics :: VendorStatistics
  }

data VendorInfo = VendorInfo
  { vendorName :: Text
  , vendorNameKana :: Text
  , corporateNumber :: Maybe CorporateNumber
  , address :: Address
  , phoneNumber :: PhoneNumber
  , email :: Maybe Email
  }

data VendorType
  = MaterialSupplier
  | ServiceProvider
  | Subcontractor
  | Distributor
  | Other

data VendorStatistics = VendorStatistics
  { totalPurchases :: Money
  , averagePurchaseValue :: Money
  , lastPurchaseDate :: Maybe Date
  , outstandingPayables :: Money
  , onTimeDeliveryRate :: Percentage
  }
```

**Commands**:
- `RegisterVendor`
- `UpdateVendorInfo`
- `SuspendVendor`
- `ReactivateVendor`

**Domain Events**:
- `VendorRegistered`
- `VendorInfoUpdated`
- `VendorSuspended`
- `VendorReactivated`

### 5. Purchase Order Aggregate

**Aggregate Root**: `PurchaseOrder`

**Invariants**:
- Must have at least one line item
- Vendor must be active
- Approval required for amounts exceeding limit
- Total must match line items

**Entities**:
```haskell
data PurchaseOrder = PurchaseOrder
  { poId :: PurchaseOrderId
  , companyId :: CompanyId
  , poNumber :: PONumber
  , vendorId :: VendorId
  , orderDate :: Date
  , requestedDeliveryDate :: Date
  , lineItems :: NonEmpty POLineItem
  , subtotal :: Money
  , consumptionTax :: Money
  , total :: Money
  , deliveryAddress :: Address
  , status :: POStatus
  , approvals :: [Approval]
  }

data POLineItem = POLineItem
  { lineItemId :: LineItemId
  , productId :: Maybe ProductId
  , description :: Text
  , quantity :: Quantity
  , unitPrice :: Money
  , lineTotal :: Money
  , taxCategory :: TaxCategory
  , requestedDate :: Date
  }

data POStatus
  = Draft
  | PendingApproval
  | Approved ApprovalInfo
  | Ordered OrderedDate
  | PartiallyReceived
  | FullyReceived
  , Cancelled

data Approval = Approval
  { approverId :: EmployeeId
  , approvalLevel :: ApprovalLevel
  , approvedAt :: Timestamp
  , comments :: Maybe Text
  }
```

### 6. Inventory Aggregate

**Aggregate Root**: `InventoryItem`

**Invariants**:
- Quantity on hand must not go negative
- Reserved quantity must not exceed available
- Location tracking must be consistent
- Lot/serial tracking enforced when required

**Entities**:
```haskell
data InventoryItem = InventoryItem
  { inventoryId :: InventoryItemId
  , companyId :: CompanyId
  , productId :: ProductId
  , warehouseId :: WarehouseId
  , quantityOnHand :: Quantity
  , quantityReserved :: Quantity
  , quantityAvailable :: Quantity
  , reorderPoint :: Maybe Quantity
  , reorderQuantity :: Maybe Quantity
  , locations :: [StorageLocation]
  , lots :: [LotInfo]
  , movements :: [InventoryMovement]
  }

data InventoryMovement = InventoryMovement
  { movementId :: MovementId
  , movementType :: MovementType
  , quantity :: Quantity
  , fromLocation :: Maybe LocationId
  , toLocation :: Maybe LocationId
  , movementDate :: Date
  , reference :: MovementReference
  , cost :: Maybe Money
  }

data MovementType
  = Receipt ReceiptInfo
  | Issue IssueInfo
  | Transfer TransferInfo
  | Adjustment AdjustmentInfo
  | Return ReturnInfo

data LotInfo = LotInfo
  { lotNumber :: LotNumber
  , quantity :: Quantity
  , receivedDate :: Date
  , expiryDate :: Maybe Date
  , cost :: Money
  }
```

## Value Objects

### Business Partner Identity
```haskell
newtype CustomerCode = CustomerCode Text
newtype VendorCode = VendorCode Text

data ContactPerson = ContactPerson
  { contactName :: PersonName
  , department :: Maybe Text
  , title :: Maybe Text
  , phoneNumber :: PhoneNumber
  , email :: Email
  , isPrimary :: Bool
  }
```

### Payment Terms
```haskell
data PaymentTerms = PaymentTerms
  { terms :: PaymentTermsType
  , description :: Text
  }

data PaymentTermsType
  = Immediate                          -- 即日
  | NetDays Days                       -- N日後
  | EndOfMonth
      { cutoffDay :: Maybe DayOfMonth  -- 締日
      , paymentDay :: DayOfMonth       -- 支払日
      }
  | MonthEnd                           -- 月末締め翌月払い
      { monthsDelay :: Int
      , paymentDay :: DayOfMonth
      }

-- Common Japanese payment terms
commonPaymentTerms :: [PaymentTerms]
commonPaymentTerms =
  [ PaymentTerms (MonthEnd 1 31) "月末締め翌月末払い"  -- Month-end close, next month-end payment
  , PaymentTerms (EndOfMonth (Just 20) 10) "20日締め翌月10日払い"  -- 20th close, next month 10th payment
  , PaymentTerms (NetDays 30) "30日後"
  ]
```

### Order Numbers
```haskell
-- Japanese order number formats often include date components
data OrderNumber = OrderNumber
  { year :: Year
  , month :: Maybe Month
  , sequence :: SequenceNumber
  }
  -- Format: 2024-03-0001

generateOrderNumber :: Date -> SequenceNumber -> OrderNumber
generateOrderNumber date seq = OrderNumber
  { year = getYear date
  , month = Just (getMonth date)
  , sequence = seq
  }
```

### Shipping
```haskell
data ShippingAddress = ShippingAddress
  { addressId :: AddressId
  , nickname :: Maybe Text           -- "Main warehouse", "Tokyo office"
  , address :: Address
  , contactPerson :: Maybe ContactPerson
  , deliveryInstructions :: Maybe Text
  , isDefault :: Bool
  }

data ShipmentInfo = ShipmentInfo
  { shipmentDate :: Date
  , carrier :: Carrier
  , trackingNumber :: Maybe TrackingNumber
  , estimatedDelivery :: Maybe Date
  , shippingMethod :: ShippingMethod
  }

data Carrier
  = YamatoTransport     -- ヤマト運輸
  | SagawaExpress       -- 佐川急便
  | JapanPost           -- 日本郵便
  | Seino               -- 西濃運輸
  | Other Text
```

## Domain Services

### 1. Order Processing Service
```haskell
class OrderProcessingService m where
  createQuotation
    :: CustomerId
    -> [OrderLineItem]
    -> m (Either QuotationError Quotation)

  convertToOrder
    :: QuotationId
    -> m (Either ConversionError SalesOrder)

  confirmOrder
    :: SalesOrder
    -> m (Either ConfirmationError ConfirmedOrder)

  reserveInventory
    :: SalesOrder
    -> m (Either ReservationError [Reservation])
```

**Business Rules**:
- Check customer credit limit before confirming
- Verify inventory availability
- Calculate consumption tax correctly
- Apply customer-specific pricing

### 2. Pricing Service
```haskell
class PricingService m where
  calculatePrice
    :: ProductId
    -> CustomerId
    -> Quantity
    -> m Money

  applyDiscounts
    :: [OrderLineItem]
    -> CustomerId
    -> m [OrderLineItem]

  calculateTax
    :: Money
    -> TaxCategory
    -> Date
    -> m TaxAmount
```

**Pricing Rules**:
- Volume discounts
- Customer-specific pricing
- Seasonal promotions
- Contract pricing

### 3. Inventory Management Service
```haskell
class InventoryManagementService m where
  recordReceipt
    :: ProductId
    -> Quantity
    -> PurchaseOrderId
    -> m InventoryMovement

  issueInventory
    :: ProductId
    -> Quantity
    -> SalesOrderId
    -> m (Either IssueError InventoryMovement)

  transferInventory
    :: ProductId
    -> Quantity
    -> LocationId
    -> LocationId
    -> m InventoryMovement

  calculateInventoryValue
    :: InventoryMethod
    -> [LotInfo]
    -> m Money
```

**Inventory Rules**:
- FIFO/LIFO/Average cost methods
- Lot tracking for perishables
- Serial number tracking for equipment
- Automatic reorder point triggers

### 4. Document Generation Service
```haskell
class DocumentGenerationService m where
  generateQuotation
    :: SalesOrder
    -> m QuotationDocument

  generateInvoice
    :: SalesOrder
    -> m InvoiceDocument

  generateDeliveryNote
    :: ShipmentInfo
    -> m DeliveryNoteDocument

  generateReceipt
    :: Payment
    -> m ReceiptDocument
```

**Japanese Document Requirements**:
- Company seal (hanko) placement
- Proper date format (和暦 or 西暦)
- Tax breakdown display
- Sequential numbering
- Legal retention requirements (7 years)

## Domain Events

```haskell
data OrderConfirmed = OrderConfirmed
  { orderId :: OrderId
  , customerId :: CustomerId
  , orderNumber :: OrderNumber
  , total :: Money
  , lineItems :: NonEmpty OrderLineItem
  , confirmedAt :: Timestamp
  }

data InventoryReserved = InventoryReserved
  { reservationId :: ReservationId
  , orderId :: OrderId
  , productId :: ProductId
  , quantity :: Quantity
  , warehouseId :: WarehouseId
  , reservedAt :: Timestamp
  }

data OrderShipped = OrderShipped
  { orderId :: OrderId
  , shipmentInfo :: ShipmentInfo
  , lineItems :: [ShippedLineItem]
  , shippedAt :: Timestamp
  }
```

## Repositories

```haskell
class CustomerRepository m where
  save :: Customer -> m ()
  findById :: CustomerId -> m (Maybe Customer)
  findByCode :: CustomerCode -> m (Maybe Customer)
  findActive :: CompanyId -> m [Customer]

class SalesOrderRepository m where
  save :: SalesOrder -> m ()
  findById :: OrderId -> m (Maybe SalesOrder)
  findByNumber :: OrderNumber -> m (Maybe SalesOrder)
  findByCustomer :: CustomerId -> DateRange -> m [SalesOrder]
  findPendingOrders :: CompanyId -> m [SalesOrder]

class InventoryRepository m where
  save :: InventoryItem -> m ()
  findByProduct :: ProductId -> WarehouseId -> m (Maybe InventoryItem)
  findLowStock :: CompanyId -> m [InventoryItem]
  findByWarehouse :: WarehouseId -> m [InventoryItem]
```

## Integration Points

### Inbound Dependencies
- **Legal Context**: Company information
- **Financial Context**: Chart of accounts, tax rates

### Outbound Integrations
- **Financial Context**: Sales → Revenue recognition, Purchases → Expense
- **HR Context**: Employee purchases, internal orders
- **Compliance Context**: Transaction reporting

## Business Rules Summary

1. **Order Processing**:
   - Quotation valid for 30 days typically
   - Credit check before order confirmation
   - Inventory reservation upon confirmation
   - Automatic invoice generation upon shipment

2. **Inventory Management**:
   - Perpetual inventory system preferred
   - Physical count reconciliation (typically annual)
   - FIFO common for cost calculation
   - Lot tracking for regulated items

3. **Invoicing**:
   - Invoice issued upon shipment
   - Payment terms per customer agreement
   - Consumption tax itemized
   - Monthly consolidated invoicing option

4. **Document Retention**:
   - Invoices: 7 years (tax law)
   - Delivery notes: 5 years
   - Quotations: 5 years
   - Purchase orders: 7 years

## Compliance Requirements

- **Commercial Code (商法)**: Contract requirements
- **Subcontract Act (下請法)**: Subcontractor payment terms
- **Consumption Tax Act**: Invoice requirements
- **Electronic Records Act**: Digital document validity
