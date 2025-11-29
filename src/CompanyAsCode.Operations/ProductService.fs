namespace CompanyAsCode.Operations

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Product/Service aggregate
module ProductService =

    open Events
    open Errors

    // ============================================
    // Product Code (Value Object)
    // ============================================

    /// Product/Service code
    type ProductCode = private ProductCode of string

    module ProductCode =

        let create (code: string) : Result<ProductCode, string> =
            if System.String.IsNullOrWhiteSpace(code) then
                Error "Product code cannot be empty"
            elif code.Length < 3 || code.Length > 20 then
                Error "Product code must be 3-20 characters"
            elif not (code |> Seq.forall (fun c -> System.Char.IsLetterOrDigit(c) || c = '-')) then
                Error "Product code must contain only letters, digits, and hyphens"
            else
                Ok (ProductCode (code.ToUpperInvariant()))

        let value (ProductCode code) = code

    // ============================================
    // Price (Value Object)
    // ============================================

    /// Price with tax handling
    type Price = {
        BasePrice: Money           // 税抜価格
        TaxRate: decimal          // 税率
        TaxIncluded: bool         // 税込表示かどうか
    }

    module Price =

        let create (basePrice: Money) (taxRate: decimal) : Price =
            {
                BasePrice = basePrice
                TaxRate = taxRate
                TaxIncluded = false
            }

        let createWithTax (priceWithTax: Money) (taxRate: decimal) : Price =
            let base' = Money.amount priceWithTax / (1m + taxRate / 100m)
            {
                BasePrice = Money.yen base'
                TaxRate = taxRate
                TaxIncluded = true
            }

        let taxAmount (price: Price) : Money =
            Money.multiply (price.TaxRate / 100m) price.BasePrice

        let totalPrice (price: Price) : Money =
            let tax = taxAmount price
            Money.add price.BasePrice tax
            |> Result.defaultValue price.BasePrice

        let displayPrice (includeTax: bool) (price: Price) : Money =
            if includeTax then totalPrice price
            else price.BasePrice

    // ============================================
    // Price History (Value Object)
    // ============================================

    /// Price change record
    type PriceHistory = {
        OldPrice: Price
        NewPrice: Price
        EffectiveDate: Date
        Reason: string option
        ChangedBy: string
        ChangedAt: DateTimeOffset
    }

    // ============================================
    // Product State
    // ============================================

    /// Product/Service state (immutable)
    type ProductState = {
        Id: ProductId
        CompanyId: CompanyId
        Code: ProductCode
        Name: string
        NameJapanese: string
        Description: string option

        // Category and type
        Category: ProductCategory
        IsService: bool              // サービスか物品か

        // Pricing
        CurrentPrice: Price
        PriceHistory: PriceHistory list

        // Status
        Status: ProductStatus

        // Service-specific
        ServiceDuration: int option  // サービス時間（分）
        RecurringInterval: string option  // 定期課金間隔

        // Metadata
        SKU: string option
        Barcode: string option
        CreatedAt: DateTimeOffset
        UpdatedAt: DateTimeOffset
    }

    module ProductState =

        let create
            (id: ProductId)
            (companyId: CompanyId)
            (code: ProductCode)
            (name: string)
            (nameJapanese: string)
            (category: ProductCategory)
            (price: Price)
            : ProductState =

            let isService =
                match category with
                | Service | Subscription | Consulting | Training | Support -> true
                | Software | Hardware | License -> false

            {
                Id = id
                CompanyId = companyId
                Code = code
                Name = name
                NameJapanese = nameJapanese
                Description = None
                Category = category
                IsService = isService
                CurrentPrice = price
                PriceHistory = []
                Status = ProductStatus.Development
                ServiceDuration = None
                RecurringInterval = None
                SKU = None
                Barcode = None
                CreatedAt = DateTimeOffset.UtcNow
                UpdatedAt = DateTimeOffset.UtcNow
            }

        let isActive (state: ProductState) =
            state.Status = ProductStatus.Active

        let isAvailable (state: ProductState) =
            state.Status = ProductStatus.Active

    // ============================================
    // Product Aggregate
    // ============================================

    /// Product/Service aggregate root
    type Product private (state: ProductState) =

        member _.State = state
        member _.Id = state.Id
        member _.CompanyId = state.CompanyId
        member _.Code = state.Code
        member _.Name = state.Name
        member _.NameJapanese = state.NameJapanese
        member _.Category = state.Category
        member _.IsService = state.IsService
        member _.CurrentPrice = state.CurrentPrice
        member _.Status = state.Status
        member _.IsActive = ProductState.isActive state
        member _.IsAvailable = ProductState.isAvailable state

        /// Get display price (with or without tax)
        member this.DisplayPrice(includeTax: bool) =
            Price.displayPrice includeTax state.CurrentPrice

        // ============================================
        // Commands
        // ============================================

        /// Launch the product (make active)
        member this.Launch()
            : Result<Product * OperationsEvent, ProductServiceError> =

            result {
                do! Result.require
                        (state.Status = ProductStatus.Development)
                        (InvalidProductCode "Product must be in development to launch")

                let newState = {
                    state with
                        Status = ProductStatus.Active
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProductCreated {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProductId = state.Id
                    ProductCode = ProductCode.value state.Code
                    ProductName = state.Name
                    Category = state.Category
                    UnitPrice = Price.totalPrice state.CurrentPrice
                }

                return (Product(newState), event)
            }

        /// Change the price
        member this.ChangePrice
            (newPrice: Price)
            (effectiveDate: Date)
            (reason: string option)
            (changedBy: string)
            : Result<Product * OperationsEvent, ProductServiceError> =

            result {
                do! Result.require
                        (state.Status <> ProductStatus.Discontinued && state.Status <> ProductStatus.EndOfLife)
                        (ProductAlreadyDiscontinued (ProductCode.value state.Code))

                do! Result.require
                        (Money.isPositive newPrice.BasePrice)
                        (InvalidPrice "Price must be positive")

                let historyEntry = {
                    OldPrice = state.CurrentPrice
                    NewPrice = newPrice
                    EffectiveDate = effectiveDate
                    Reason = reason
                    ChangedBy = changedBy
                    ChangedAt = DateTimeOffset.UtcNow
                }

                let newState = {
                    state with
                        CurrentPrice = newPrice
                        PriceHistory = state.PriceHistory @ [historyEntry]
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = PriceChanged {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProductId = state.Id
                    OldPrice = Price.totalPrice state.CurrentPrice
                    NewPrice = Price.totalPrice newPrice
                    EffectiveDate = effectiveDate
                }

                return (Product(newState), event)
            }

        /// Discontinue the product
        member this.Discontinue()
            : Result<Product, ProductServiceError> =

            result {
                do! Result.require
                        (state.Status = ProductStatus.Active)
                        (ProductAlreadyDiscontinued (ProductCode.value state.Code))

                return Product({
                    state with
                        Status = ProductStatus.Discontinued
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark as end of life
        member this.MarkEndOfLife()
            : Result<Product, ProductServiceError> =

            result {
                do! Result.require
                        (state.Status = ProductStatus.Discontinued)
                        (InvalidProductCode "Product must be discontinued first")

                return Product({
                    state with
                        Status = ProductStatus.EndOfLife
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Set service duration (for services)
        member this.SetServiceDuration(durationMinutes: int)
            : Result<Product, ProductServiceError> =

            result {
                do! Result.require
                        state.IsService
                        (InvalidServiceDuration "Can only set duration for service products")

                do! Result.require
                        (durationMinutes > 0)
                        (InvalidServiceDuration "Duration must be positive")

                return Product({
                    state with
                        ServiceDuration = Some durationMinutes
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Set recurring interval (for subscriptions)
        member this.SetRecurringInterval(interval: string)
            : Result<Product, ProductServiceError> =

            result {
                do! Result.require
                        (state.Category = Subscription)
                        (InvalidProductCode "Can only set interval for subscription products")

                let validIntervals = ["monthly"; "quarterly"; "yearly"; "monthly/年払い"; "yearly/年払い"]
                do! Result.require
                        (validIntervals |> List.exists (fun i -> i.ToLower() = interval.ToLower()))
                        (InvalidServiceDuration "Invalid recurring interval")

                return Product({
                    state with
                        RecurringInterval = Some interval
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Update description
        member this.UpdateDescription(description: string)
            : Product =
            Product({ state with Description = Some description; UpdatedAt = DateTimeOffset.UtcNow })

        /// Set SKU
        member this.SetSKU(sku: string)
            : Product =
            Product({ state with SKU = Some sku; UpdatedAt = DateTimeOffset.UtcNow })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new product
        static member Create
            (companyId: CompanyId)
            (code: string)
            (name: string)
            (nameJapanese: string)
            (category: ProductCategory)
            (basePrice: Money)
            (taxRate: decimal)
            : Result<Product, ProductServiceError> =

            result {
                let! productCode =
                    ProductCode.create code
                    |> Result.mapError InvalidProductCode

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidProductName "Product name cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(nameJapanese)))
                        (InvalidProductName "Japanese name cannot be empty")

                do! Result.require
                        (Money.isPositive basePrice)
                        (InvalidPrice "Price must be positive")

                let price = Price.create basePrice taxRate
                let productId = ProductId.create()
                let state = ProductState.create productId companyId productCode name nameJapanese category price

                return Product(state)
            }

        /// Create a service
        static member CreateService
            (companyId: CompanyId)
            (code: string)
            (name: string)
            (nameJapanese: string)
            (hourlyRate: Money)
            (taxRate: decimal)
            : Result<Product, ProductServiceError> =

            result {
                let! product = Product.Create companyId code name nameJapanese Service hourlyRate taxRate
                return product
            }

        /// Reconstitute from state
        static member FromState(state: ProductState) : Product =
            Product(state)

    // ============================================
    // Product Logic
    // ============================================

    module ProductLogic =

        /// Calculate price for quantity
        let calculateTotal (quantity: decimal) (product: Product) : Money =
            let unitPrice = Price.totalPrice product.CurrentPrice
            Money.multiply quantity unitPrice

        /// Get active products
        let activeProducts (products: Product list) : Product list =
            products |> List.filter (fun p -> p.IsActive)

        /// Filter by category
        let byCategory (category: ProductCategory) (products: Product list) : Product list =
            products |> List.filter (fun p -> p.Category = category)

        /// Get price history for audit
        let priceHistory (product: Product) : (Date * Money * Money) list =
            product.State.PriceHistory
            |> List.map (fun h ->
                (h.EffectiveDate,
                 Price.totalPrice h.OldPrice,
                 Price.totalPrice h.NewPrice))
