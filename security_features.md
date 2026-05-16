# Coretex Tactical Security Features

This document outlines the professional-grade security infrastructure implemented in the **Coretex Executive Decision Support System**. These features are designed to protect corporate data and ensure strict operational governance.

## 1. Multi-Factor Authentication (MFA)
*   **Mandatory Enforcement:** Every user role (from Cashier to CEO) is strictly required to pass a secondary identity verification challenge. There are no "bypass" routes for internal testing.
*   **Email-Based OTP:** Verification codes are dispatched via a secure, branded HTML email system immediately upon successful password entry.

## 2. Dynamic PIN Uniqueness (Rotation)
*   **Security Stamp Rotation:** Every time a user hits the login screen, the system force-rotates their cryptographic security stamp.
*   **Impact:** This ensures that every single login attempt generates a **unique** PIN, even if multiple attempts occur within the same time window.

## 3. One-Time Use Logic (PIN Consumption)
*   **Replay Attack Protection:** The moment a PIN is successfully verified, the system "consumes" it by rotating the security stamp again.
*   **Impact:** A code can never be used twice. Even if a user logs out and tries to use the same code 10 seconds later, it will be rejected as invalid.

## 4. Temporal Constraints (Strict TTL)
*   **2-Minute Lifespan:** To minimize the "window of opportunity" for attackers, all OTP tokens have a strict Time-To-Live (TTL) of **120 seconds**. 
*   **Impact:** Expired codes are mathematically rejected by the Identity provider, forcing a fresh request.

## 5. Brute-Force Mitigation (Adaptive Throttling)
*   **Penalty Delays:** Beyond just locking accounts, the system implements **Adaptive Throttling**. Starting at the 3rd failed attempt, the server introduces a progressive delay (2-4 seconds) before responding.
*   **Impact:** This "chokes" automated brute-force bots, making credential-stuffing attacks mathematically impossible without impacting real users.

## 6. Password Strength & Entropy Management
*   **Tactical Strength Meter:** During password updates, users are guided by a real-time visual assessment tool that measures "Entropy" (complexity and randomness).
*   **NIST Compliance Enforcement:** The system physically blocks users from setting weak credentials, requiring a "Tactical Grade" assessment for all personnel.

## 7. Administrative Governance (The Master Key)
*   **Manual Unlock Override:** In urgent operational scenarios, a Master Admin or CEO can manually override a system lockout via the **Staff Management Dashboard**.
*   **Locked-Out Filter:** A dedicated security view allows administrators to instantly isolate and manage all personnel currently under system lockout.

## 7. Operational Audit Logging
*   **Full Transparency:** Every security event is recorded in the **Activity Log**, including:
    *   Failed PIN attempts.
    *   System-triggered account lockouts.
    *   Manual administrative unlocks.
*   **Audit Trail:** This provides a tamper-proof ledger of all security-related interactions.

## 10. High-Entropy Provisioning
*   **Randomized Credentials:** All auto-generated passwords follow a high-entropy randomization algorithm rather than predictable patterns.
*   **Direct Delivery:** New credentials are dispatched directly to the employee's encrypted inbox, ensuring no one (including the Admin) has permanent access to the plain-text password.

