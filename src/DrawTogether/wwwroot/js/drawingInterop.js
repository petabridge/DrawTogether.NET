// Drawing Surface Interop Functions
export const drawingInterop = {
    // Store references to drawing components
    components: {},
    
    // Initialize a drawing surface with event listeners
    initialize: function (dotNetHelper, containerId, svgElement) {
        this.components[containerId] = {
            dotNetRef: dotNetHelper,
            svgElement: svgElement
        };
        
        // Add window resize listener to handle coordinate system changes
        window.addEventListener('resize', () => {
            // Refresh SVG coordinate system on resize
            this.refreshSvgMatrix(containerId);
        });
        
        // Initial matrix calculation
        this.refreshSvgMatrix(containerId);

        return true;
    },
    
    // Update the SVG transformation matrix (call after resize)
    refreshSvgMatrix: function (containerId) {
        const component = this.components[containerId];
        if (!component) return;
        
        // We'll calculate this on demand
    },
    
    // Transform client coordinates to SVG viewBox coordinates
    transformCoordinates: function (svgElement, clientX, clientY) {
        try {
            // Get the SVG element's CTM
            const svg = svgElement;
            
            // Create an SVG point using client coordinates
            const pt = new DOMPoint();
            pt.x = clientX;
            pt.y = clientY;
            
            // Get the current transform matrix and its inverse
            const ctm = svg.getScreenCTM();
            if (!ctm) return [clientX, clientY]; // Fallback
            
            const inverseCTM = ctm.inverse();
            
            // Transform the point from screen coordinates to SVG coordinates
            const svgPoint = pt.matrixTransform(inverseCTM);
            
            // Return the transformed coordinates
            return [svgPoint.x, svgPoint.y];
        } catch (error) {
            console.error("Error transforming coordinates:", error);
            // Fallback to direct coordinates in case of error
            return [clientX, clientY];
        }
    },
    
    // Clean up when component is disposed
    dispose: function (containerId) {
        if (this.components[containerId]) {
            delete this.components[containerId];
        }
        return true;
    }
}; 