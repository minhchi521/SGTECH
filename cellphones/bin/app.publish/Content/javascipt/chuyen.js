document.addEventListener('DOMContentLoaded', function () {
    const bookGrid = document.querySelector('.book-grid');

    if (!bookGrid) {
        console.error('Không tìm thấy element .book-grid');
        return;
    }

    bookGrid.addEventListener('click', function (event) {
        const clickedElement = event.target;
        console.log('Clicked element:', clickedElement);

        if (clickedElement.classList.contains('book-image')) {
            const IDtruyen = clickedElement.getAttribute('dat-idtruyen');
            console.log('ID truyen:', IDtruyen);

            if (IDtruyen) {
                const url = `/Product/DetailProduct/${IDtruyen}`;
                console.log('Redirecting to:', url);
                window.location.href = url;
            }
        }
    });
});