/**
 * DaNang SafeMap - Main JavaScript
 * Handles navigation, map, and interactions
 */

// ===== Initialize Lucide Icons =====
document.addEventListener('DOMContentLoaded', () => {
    lucide.createIcons();
    initMobileMenu();
    initMap();
});

// ===== Mobile Menu =====
function initMobileMenu() {
    const menuBtn = document.getElementById('mobileMenuBtn');
    const mobileMenu = document.getElementById('mobileMenu');

    if (menuBtn && mobileMenu) {
        menuBtn.addEventListener('click', () => {
            mobileMenu.classList.toggle('hidden');

            // Animate icon
            const icon = menuBtn.querySelector('[data-lucide]');
            if (mobileMenu.classList.contains('hidden')) {
                icon.setAttribute('data-lucide', 'menu');
            } else {
                icon.setAttribute('data-lucide', 'x');
            }
            lucide.createIcons();
        });
    }
}

// ===== Leaflet Map =====
function initMap() {
    const mapContainer = document.getElementById('mapContainer');
    if (!mapContainer) return;

    // Da Nang coordinates
    const daNangCenter = [16.0544, 108.2022];

    // Initialize map
    const map = L.map('mapContainer', {
        center: daNangCenter,
        zoom: 13,
        zoomControl: false
    });

    // Add zoom control to bottom right
    L.control.zoom({ position: 'bottomright' }).addTo(map);

    // Dark theme tile layer
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
        subdomains: 'abcd',
        maxZoom: 19
    }).addTo(map);

    // Sample alert data
    const sampleAlerts = [
        {
            id: 1,
            title: 'Cướp giật tại đường Nguyễn Văn Linh',
            type: 'theft',
            category: 'Xâm phạm sở hữu',
            lat: 16.0600,
            lng: 108.2200,
            time: '2 giờ trước',
            verified: 8,
            description: 'Đối tượng đi xe máy giật điện thoại của người đi bộ'
        },
        {
            id: 2,
            title: 'Đua xe trái phép tại cầu Rồng',
            type: 'disorder',
            category: 'Trật tự công cộng',
            lat: 16.0610,
            lng: 108.2280,
            time: '4 giờ trước',
            verified: 12,
            description: 'Nhóm thanh niên tụ tập đua xe vào ban đêm'
        },
        {
            id: 3,
            title: 'Chèo kéo du khách tại bãi biển Mỹ Khê',
            type: 'scam',
            category: 'An ninh du lịch',
            lat: 16.0480,
            lng: 108.2450,
            time: '1 giờ trước',
            verified: 5,
            description: 'Người bán hàng rong chèo kéo và đòi giá cao'
        },
        {
            id: 4,
            title: 'Móc túi tại chợ Hàn',
            type: 'theft',
            category: 'Xâm phạm sở hữu',
            lat: 16.0680,
            lng: 108.2230,
            time: '6 giờ trước',
            verified: 15,
            description: 'Nạn nhân bị móc ví trong lúc mua sắm đông người'
        },
        {
            id: 5,
            title: 'Lừa đảo taxi giá cao',
            type: 'scam',
            category: 'An ninh du lịch',
            lat: 16.0560,
            lng: 108.2100,
            time: '3 giờ trước',
            verified: 7,
            description: 'Taxi không bật đồng hồ, tính giá cao gấp 3 lần'
        }
    ];

    // Add markers
    sampleAlerts.forEach(alert => {
        const marker = createCustomMarker(alert);
        marker.addTo(map);
    });

    // Time filter
    const timeFilter = document.getElementById('timeFilter');
    if (timeFilter) {
        timeFilter.addEventListener('change', (e) => {
            console.log('Filter by:', e.target.value, 'hours');
            // In real app: fetch filtered data from API
        });
    }
}

// Create custom marker
function createCustomMarker(alert) {
    const colorMap = {
        'theft': '#EF4444',    // Red
        'disorder': '#F97316', // Orange
        'scam': '#EAB308'      // Yellow
    };

    const iconMap = {
        'theft': 'alert-triangle',
        'disorder': 'zap',
        'scam': 'alert-circle'
    };

    // Create custom icon
    const icon = L.divIcon({
        className: 'custom-marker-wrapper',
        html: `
            <div class="relative">
                <div class="w-10 h-10 rounded-full flex items-center justify-center shadow-lg animate-pulse" 
                     style="background: ${colorMap[alert.type]}; box-shadow: 0 0 20px ${colorMap[alert.type]}40;">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-white" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        ${getIconPath(alert.type)}
                    </svg>
                </div>
                <div class="absolute -bottom-1 left-1/2 -translate-x-1/2 w-0 h-0 border-l-4 border-r-4 border-t-8 border-l-transparent border-r-transparent" 
                     style="border-top-color: ${colorMap[alert.type]};"></div>
            </div>
        `,
        iconSize: [40, 50],
        iconAnchor: [20, 50],
        popupAnchor: [0, -50]
    });

    // Create marker with popup
    const marker = L.marker([alert.lat, alert.lng], { icon });

    // Popup content
    const popupContent = `
        <div class="min-w-[280px]">
            <div class="flex items-start gap-3 mb-3">
                <div class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0" 
                     style="background: ${colorMap[alert.type]}20;">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" style="color: ${colorMap[alert.type]}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        ${getIconPath(alert.type)}
                    </svg>
                </div>
                <div>
                    <h3 class="font-semibold text-white text-sm">${alert.title}</h3>
                    <p class="text-xs text-gray-400">${alert.category} • ${alert.time}</p>
                </div>
            </div>
            <p class="text-sm text-gray-300 mb-4">${alert.description}</p>
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-xs text-gray-400">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-green-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M20 6 9 17l-5-5"/>
                    </svg>
                    <span>${alert.verified} người xác nhận</span>
                </div>
                <button onclick="verifyAlert(${alert.id})" 
                        class="px-3 py-1.5 bg-primary-500/20 hover:bg-primary-500/30 text-primary-400 text-xs font-medium rounded-lg transition-colors">
                    Xác nhận
                </button>
            </div>
        </div>
    `;

    marker.bindPopup(popupContent, {
        maxWidth: 350,
        className: 'custom-popup'
    });

    return marker;
}

// Get SVG path for icon
function getIconPath(type) {
    const paths = {
        'theft': '<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" x2="12" y1="9" y2="13"/><line x1="12" x2="12.01" y1="17" y2="17"/>',
        'disorder': '<polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>',
        'scam': '<circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/>'
    };
    return paths[type] || paths['theft'];
}

// Verify alert (demo function)
function verifyAlert(alertId) {
    console.log('Verifying alert:', alertId);
    alert('Cảm ơn bạn đã xác nhận! Bạn cần đăng nhập để xác thực cảnh báo.');
}

// ===== Smooth Scroll =====
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// ===== Header scroll effect =====
let lastScroll = 0;
window.addEventListener('scroll', () => {
    const header = document.querySelector('header');
    const currentScroll = window.pageYOffset;

    if (currentScroll > 100) {
        header.style.top = currentScroll > lastScroll ? '-100px' : '1rem';
    } else {
        header.style.top = '1rem';
    }

    lastScroll = currentScroll;
});
