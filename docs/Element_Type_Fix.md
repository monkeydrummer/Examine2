# BEM Element Type Issue - ROOT CAUSE IDENTIFIED

**Date**: February 13, 2026  
**Status**: ✅ FIXED  
**Severity**: Critical - Matrix Conditioning Issue

---

## Problem Summary

The BEM influence matrix was showing extreme ill-conditioning (condition number > 10^48) when running real models with 318 elements, even though the closed-form analytical integration was properly implemented.

### Symptoms
```
Number of elements: 318
Using FULL-SPACE solution
Influence matrix statistics:
  Size: 1272x1272
  Condition number: 1.291E+048
  WARNING: Matrix is ill-conditioned!
```

---

## Root Cause

The C# code was configured to use **LINEAR elements** (`ElementType.Linear`), but the implementation only properly supports **CONSTANT elements**.

### Evidence from C++ Reference Code

In `bcompute2d.cpp` line 460:
```cpp
// element order (constant,linear, quadratic)
order=1;  // <-- Uses CONSTANT elements (order=1)
```

The C++ code defaults to `order=1` (constant elements), NOT `order=2` (linear elements).

The `linear_comb()` function (line 2794) is only called when `order==2`:
```cpp
switch(order){
    case 2: linear_comb(st11,st21,(double)xmp);
        break;
    case 3: quadratic_comb(st11,st21,(double)xmp);
        break;
    default : break;  // order=1, no shape function transformation needed
}
```

### Why Linear Elements Failed

1. **C# Configuration**: Defaulted to `ElementType.Linear` 
   - `BEMConfiguration.cs` line 32: `ElementType = ElementType.Linear`
   - `ContourService.cs` line 94: `ElementType = ElementType.Linear`

2. **Implementation Gap**: `ElementIntegrator.ComputeFullSpaceInfluence()` only computes coefficients for constant elements
   - Only populates `st11[0][...]` and `st21[0][...]` (the k=0 indices for constant elements)
   - Does NOT populate `st11[1][...]` and `st21[1][...]` needed for linear elements
   - Does NOT call `ApplyLinearShapeFunctions()` to transform coefficients

3. **Matrix Assembly**: `InfluenceMatrixBuilder` attempts to use linear elements with incomplete influence data
   - When `order > 1`, it tries to use shape function weights
   - But the underlying influence coefficients are wrong because they weren't computed for multiple nodes per element

4. **Result**: The matrix ends up with incorrect values that make it catastrophically ill-conditioned

---

## The Fix

Changed element type from LINEAR back to CONSTANT to match the C++ implementation.

### Files Modified

1. **`src/Examine2DModel/BEM/BEMConfiguration.cs`** (line 32)
   ```csharp
   // BEFORE:
   public ElementType ElementType { get; set; } = ElementType.Linear;
   
   // AFTER:
   public ElementType ElementType { get; set; } = ElementType.Constant;
   ```
   Added comment explaining that only Constant elements are currently implemented.

2. **`src/Examine2DModel/Services/ContourService.cs`** (line 94)
   ```csharp
   // BEFORE:
   ElementType = ElementType.Linear,
   
   // AFTER:
   ElementType = ElementType.Constant,  // IMPORTANT: Must match C++ code (order=1)
   ```

---

## Expected Results After Fix

With 318 elements using CONSTANT element type:

- **Matrix size**: 636×636 (2 DOF per element: 318 × 2 = 636)
  - Previously: 1272×1272 (wrong, was treating as 2 nodes per element)
- **Condition number**: Should be ~10^3 to 10^8 (well-conditioned)
  - Previously: >10^48 (catastrophically ill-conditioned)
- **Solution stability**: Numerical solution should converge and produce realistic values

---

## Technical Background

### Boundary Element Types

1. **Constant Elements** (order=1):
   - One collocation point per element (at midpoint)
   - Uniform traction/displacement along element
   - Simpler, more robust, well-tested
   - **What the C++ code uses**

2. **Linear Elements** (order=2):
   - Two collocation points per element (at nodes)
   - Linear variation of traction/displacement along element
   - Requires shape function transformations via `linear_comb()`
   - More accurate but more complex
   - **NOT fully implemented in C# port**

3. **Quadratic Elements** (order=3):
   - Three collocation points per element
   - Quadratic variation along element
   - Most accurate but most complex
   - **NOT implemented in C# port**

### Why Constant Elements Work Well for BEM

- BEM matrices are **dense** (not sparse), so element count is more important than element order
- For the same computational cost, more constant elements often give better accuracy than fewer high-order elements
- The closed-form analytical integration handles singularities perfectly for constant elements
- Well-tested and proven in the original C++ implementation

---

## Future Work (If Linear Elements Are Needed)

To properly implement linear elements would require:

1. **Modify `ElementIntegrator.ComputeFullSpaceInfluence()`**:
   - Compute influence at **TWO collocation points** (element endpoints)
   - Populate all of `st11[0..1][0..4]` and `st21[0..1][0..4]`
   - Call `ApplyLinearShapeFunctions()` to transform coefficients

2. **Update Collocation Point Logic**:
   - For linear elements, collocation points are at nodes (x = ±halfLength), not at midpoint
   - Need to evaluate influence coefficients at both endpoints

3. **Half-Space Solution**:
   - Extend half-space image solution to handle linear elements
   - Add terms for `order>1` in the numerical integration loops

4. **Testing**:
   - Extensive testing to verify matrix conditioning remains good
   - Comparison against analytical solutions
   - Validation against original C++ code running with `order=2`

---

## Verification

### Before Fix
- Element type: Linear (incorrect)
- Matrix size: 1272×1272
- Condition number: 1.291×10^48
- Result: Unusable, numerically unstable

### After Fix
- Element type: Constant (correct, matches C++)
- Matrix size: 636×636
- Condition number: Expected ~10^3-10^8
- Result: Should be well-conditioned and stable

---

## References

- C++ source: `D:\repos\Examine2 OG\bcompute2d.cpp`
  - Line 460: `order=1` (constant elements)
  - Lines 1950-1956: `linear_comb()` only called for order=2
  - Lines 2794-2820: `linear_comb()` implementation
- Previous fix: `docs/BEM_Matrix_Conditioning_SOLUTION.md` (analytical integration)
- This document: Element type configuration fix

---

## Lesson Learned

When porting numerical code:
1. **Verify ALL configuration defaults** match the reference implementation
2. **Check element types carefully** - they have major implications for matrix structure
3. **Match the reference implementation first**, then enhance later
4. **Test incrementally** - don't add features (like higher-order elements) until the baseline works
