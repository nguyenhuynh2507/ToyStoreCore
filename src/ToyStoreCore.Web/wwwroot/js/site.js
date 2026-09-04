// Password Toggle Functionality
console.log('Site.js loaded');

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing password toggle');
    initializePasswordToggle();
});

function initializePasswordToggle() {
    console.log('Initializing password toggle...');
    
    // Get all password toggle buttons
    const toggleButtons = document.querySelectorAll('.password-toggle-btn');
    console.log('Found toggle buttons:', toggleButtons.length);
    
    toggleButtons.forEach((button, index) => {
        console.log(`Setting up button ${index}:`, button);
        
        button.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Toggle button clicked');
            
            const targetId = this.getAttribute('data-target');
            console.log('Target ID:', targetId);
            
            const targetInput = document.getElementById(targetId);
            console.log('Target input:', targetInput);
            
            const icon = this.querySelector('.password-toggle-icon');
            console.log('Icon:', icon);
            
            if (targetInput && icon) {
                togglePasswordVisibility(targetInput, icon, this);
            } else {
                console.error('Missing target input or icon');
            }
        });
        
        // Add hover effects
        button.addEventListener('mouseenter', function() {
            const icon = this.querySelector('.password-toggle-icon');
            if (icon) {
                icon.style.transform = 'scale(1.2) rotate(5deg)';
            }
        });
        
        button.addEventListener('mouseleave', function() {
            const icon = this.querySelector('.password-toggle-icon');
            if (icon) {
                icon.style.transform = 'scale(1) rotate(0deg)';
            }
        });
    });
}

function togglePasswordVisibility(input, icon, button) {
    console.log('Toggling password visibility');
    console.log('Current input type:', input.type);
    
    // Toggle input type
    if (input.type === 'password') {
        input.type = 'text';
        icon.className = 'bi bi-eye-slash password-toggle-icon';
        console.log('Password shown');
        
        // Add rotation animation
        icon.classList.add('rotating');
        setTimeout(() => {
            icon.classList.remove('rotating');
        }, 400);
        
        // Add pulse effect
        button.classList.add('pulse');
        setTimeout(() => {
            button.classList.remove('pulse');
        }, 500);
        
        // Update button title for accessibility
        button.setAttribute('title', 'Ẩn mật khẩu');
        
    } else {
        input.type = 'password';
        icon.className = 'bi bi-eye password-toggle-icon';
        console.log('Password hidden');
        
        // Add rotation animation
        icon.classList.add('rotating');
        setTimeout(() => {
            icon.classList.remove('rotating');
        }, 400);
        
        // Add pulse effect
        button.classList.add('pulse');
        setTimeout(() => {
            button.classList.remove('pulse');
        }, 500);
        
        // Update button title for accessibility
        button.setAttribute('title', 'Hiện mật khẩu');
    }
    
    // Focus back to input for better UX
    input.focus();
}