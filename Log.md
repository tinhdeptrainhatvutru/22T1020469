# Tạo solution và các project ạBlank Solitin Có th S-MsV (ví dụ: SV22T1020001)
Bổ sung cho solution các project sau:
- ‹SolutionName> .Admin: project dạng ASP. NET Core MVC (ví dụ: SV22T1020001. Admin)
- ‹SolutionName>. Shop: project dạng ASP. NET Core MVC
- ‹SolutionName> .Models: project dạng Class Library
- ‹SolutionName>. Datalayers: project dạng Class Library
- ‹SolutionName>. BusinessLayers: project dạng Class Library

# Thiết kế Layout cho App Admin

- Sử dụng Theme AdmninLTE4, Boostrap5

# Các controller và Action dự kiến (Chức năng dự kiến)

## Home
- Home/Index

## Account
- Account/Login
- Account/Logout
- Account/ChangePassword

## Supplier
- Supplier/Index
	- Hiển thị danh sách nhà cung cấp dưới dạng phân trang
	- Tim kiếm nhà cung cấp theo tên
- Supplier/Create
- Supplier/Edit/{id}
- Supplier/Delete/{id}

## Customer
- Customer/Index
- Hiển thị danh sách khách hàng dưới dạng phân trang
- Tìm kiếm khách hàng theo tên
- Điều hướng đến các chức năng khác liên quan đến khách hàng
- Customer/Create
- Customer/Edit/{id}
- Customer/Delete/{id}
- Customer/ChangePassword/{id}

## Shipper
- Shipper/Index
- Shipper/Create
- Shipper/Edit/{id}
- Shipper/Delete/{id}

## Employee
- Employee/Index
- Employee/Create
- Employee/Edit/{id}
- Employee/Delete/{id}
- Employee/ChangePassword/{id}
- Employee/ChangeRole/{id}

## Category
- Category/Index
- Category/Create
- Category/Edit/{id}
- Category/Delete/{id}

## Product
- Product/Index
- Product/Create
- Product/Edit/{id}
- Product/Delete/{id}
- Product/Detail/{id}
- Product/ListAttribute/{id}
- Product/CreateAttribute/{id}
- Product/EditAttribute/{id}?attributeId={attributeId}
- Product/DeleteAttribute/{id}?attributeId={attributeId}
- Product/ListPhotos/{id}
- Product/CreatePhoto/{id}
- Product/DeletePhoto/{id}?photoId={photoId}

## Order
- Order/Index
- Order/Create
- ...