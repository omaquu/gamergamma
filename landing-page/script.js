// 3D Parallax Mouse Tracking Effect
document.addEventListener('DOMContentLoaded', function () {
    // Apply to all parallax scenes
    const parallaxScenes = ['parallaxScene', 'advancedParallax', 'curveParallax'];
    
    parallaxScenes.forEach(sceneId => {
        const parallaxScene = document.getElementById(sceneId);
        
        if (parallaxScene) {
            const layers = parallaxScene.querySelectorAll('.parallax-layer');

            // Mouse move parallax effect
            document.addEventListener('mousemove', function (e) {
                const mouseX = e.clientX / window.innerWidth;
                const mouseY = e.clientY / window.innerHeight;

                layers.forEach(layer => {
                    const depth = layer.getAttribute('data-depth') || 0.5;
                    const moveX = (mouseX - 0.5) * depth * 50;
                    const moveY = (mouseY - 0.5) * depth * 50;

                    layer.style.transform = `
                        translateX(${moveX}px) 
                        translateY(${moveY}px)
                        rotateY(${(mouseX - 0.5) * depth * 20}deg)
                        rotateX(${-(mouseY - 0.5) * depth * 20}deg)
                    `;
                });
            });

            // Device orientation parallax for mobile
            if (window.DeviceOrientationEvent) {
                window.addEventListener('deviceorientation', function (e) {
                    const tiltX = e.gamma; // -90 to 90
                    const tiltY = e.beta;  // -180 to 180

                    layers.forEach(layer => {
                        const depth = layer.getAttribute('data-depth') || 0.5;
                        const moveX = (tiltX / 90) * depth * 30;
                        const moveY = (tiltY / 180) * depth * 30;

                        layer.style.transform = `
                            translateX(${moveX}px) 
                            translateY(${moveY}px)
                            rotateY(${(tiltX / 90) * depth * 15}deg)
                            rotateX(${-(tiltY / 180) * depth * 15}deg)
                        `;
                    });
                });
            }
        }
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href.length > 1) {
                e.preventDefault();
                const target = document.querySelector(href);
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });

    // Add scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Observe feature cards and section content
    document.querySelectorAll('.feature-card, .philosophy-content, .tactical-content').forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(el);
    });

    // Animated counter for stats
    const animateValue = (element, start, end, duration) => {
        let startTimestamp = null;
        const step = (timestamp) => {
            if (!startTimestamp) startTimestamp = timestamp;
            const progress = Math.min((timestamp - startTimestamp) / duration, 1);

            if (typeof end === 'number') {
                element.textContent = Math.floor(progress * (end - start) + start);
            } else {
                element.textContent = end; // For non-numeric values like ∞
            }

            if (progress < 1) {
                window.requestAnimationFrame(step);
            }
        };
        window.requestAnimationFrame(step);
    };

    // Observer for stat animation
    const statsObserver = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const statValue = entry.target;
                const finalValue = statValue.textContent;

                if (finalValue !== '∞') {
                    const numValue = parseInt(finalValue);
                    if (!isNaN(numValue)) {
                        animateValue(statValue, 0, numValue, 1500);
                    }
                }

                statsObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.5 });

    document.querySelectorAll('.stat-value').forEach(stat => {
        statsObserver.observe(stat);
    });
});

// Add header scroll effect
let lastScroll = 0;
window.addEventListener('scroll', () => {
    const header = document.querySelector('.header');
    const currentScroll = window.pageYOffset;

    if (currentScroll > 100) {
        header.style.background = 'rgba(10, 10, 15, 0.95)';
    } else {
        header.style.background = 'rgba(10, 10, 15, 0.8)';
    }

    lastScroll = currentScroll;
});
