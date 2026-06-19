# 🍽️ Sajik Online Restaurant App

Sajik Online Restaurant App is a full-stack web application developed using ASP.NET Core MVC, Entity Framework Core, and SQL Server. The application is designed to provide a seamless online food ordering experience for customers while offering powerful management capabilities for administrators.

The system follows a role-based architecture with two user types: **Admin** and **Customer**. Customers can register, log in, browse food items, add products to their cart, and manage their orders, while administrators can manage restaurant data including categories, food items, sliders, and customer accounts through a secure dashboard.

---

## 📌 Features

### 👤 Customer Features
- User Registration and Login
- Session-Based Authentication
- Food Item Browsing
- Category-Based Navigation
- Search Functionality
- Shopping Cart Management
- Checkout Process
- Profile Management
- Order Tracking

### 🛠️ Admin Features
- Secure Admin Dashboard
- Customer Management
- Enable/Disable Customer Accounts
- Category Management
- Food Item Management
- Slider/Banner Management
- Role-Based Access Control

---

## 🏗️ Architecture

The application follows the **Model-View-Controller (MVC)** design pattern:

- **Models** – Represent entities and database relationships.
- **Views** – Razor pages responsible for the user interface.
- **Controllers** – Handle business logic and user requests.
- **Entity Framework Core** – Used for database operations with SQL Server.

---

## 🛠️ Tech Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap 5
- JavaScript
- jQuery

---

## 🔐 Authentication & Authorization

The application implements session-based authentication and role-based authorization.

### Roles
- Admin
- Customer

Based on the authenticated user's role:
- Admins are redirected to the Admin Dashboard.
- Customers are redirected to the User Home Page.

---

## 📂 Key Modules

### User Management
- Registration
- Login & Logout
- Customer Account Status Management
- Role Assignment

### Restaurant Management
- Categories
- Food Items
- Sliders

### Customer Experience
- Browse Menu
- Search Dishes
- Cart Operations
- Checkout Process

---

## 🎯 Future Enhancements

- Online Payment Gateway Integration
- ASP.NET Identity Authentication
- Password Hashing and Enhanced Security
- Order Tracking System
- Email Notifications
- REST API Integration
- Mobile Application Support

---

## 👨‍💻 Developer

**Mohammad Irfan**  
Programmer Analyst Trainee | Cognizant

---
⭐ If you found this project useful, consider giving it a star!
