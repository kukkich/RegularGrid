import json
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors
import numpy as np
import matplotlib.pyplot as plt
from matplotlib import patches
from scipy.interpolate import griddata
import csv

# Функция для чтения JSON-файла
def load_grid_from_json(filename):
    with open(filename, 'r') as f:
        return json.load(f)

def parse_float(value, decimal_separator):
    """Парсить строку с учетом разделителя между целой и дробной частью."""
    if decimal_separator == ',':
        value = value.replace(',', '.')
    return float(value)

def load_solution(file_path, decimal_separator='.'):
    """Чтение данных из файла и преобразование их в массивы x, y, T."""
    x_vals = []
    y_vals = []
    t_vals = []

    with open(file_path, 'r') as file:
        reader = csv.reader(file, delimiter=' ')
        for row in reader:
            if len(row) != 3:
                continue  # Пропустить некорректные строки
            x = parse_float(row[0], decimal_separator)
            y = parse_float(row[1], decimal_separator)
            # T = x + y + 1
            T = parse_float(row[2], decimal_separator)
            x_vals.append(x)
            y_vals.append(y)
            t_vals.append(T)

    return np.array(x_vals), np.array(y_vals), np.array(t_vals)

def load_edges_from_file(filename):
    edges = []
    with open(filename, 'r') as f:
        for line in f:
            # Каждая строка состоит из трёх чисел: первая вершина, вторая вершина, номер ребра
            node1, node2, edge_id = map(int, line.split())
            edges.append((node1, node2, edge_id))
    return edges

def plot_grid_with_edges(grid_data, edges_data):
    points = grid_data['Nodes']
    elements = grid_data['Elements']

    # Создаём палитру цветов на основе AreaId
    colors = list(mcolors.TABLEAU_COLORS.values())  # Используем таблицу цветов из matplotlib

    # Для каждого элемента отрисуем его и закрасим в зависимости от AreaId, а также добавим номер элемента
    for element_id, element in enumerate(elements):
        node_ids = element['NodeIds']
        area_id = element['AreaId']

        # Получаем координаты узлов элемента в правильном порядке
        # Порядок: 0-я, 1-я, 3-я, 2-я
        element_points = [points[node_ids[i]] for i in [0, 1, 3, 2]]

        # Разделяем координаты X и Y
        x_coords = [p['X'] for p in element_points]
        y_coords = [p['Y'] for p in element_points]

        # Определяем цвет для элемента в зависимости от AreaId
        color = colors[area_id % len(colors)]  # Используем модуль, чтобы избежать выхода за пределы палитры

        # Закрашиваем элемент
        plt.fill(x_coords, y_coords, color=color, edgecolor='black', alpha=0.5)

        # Вычисляем центр элемента для размещения номера элемента
        center_x = sum(x_coords) / 4
        center_y = sum(y_coords) / 4

        # Добавляем текст с номером элемента в центр
        plt.text(center_x, center_y, f'{element_id}', fontsize=10, color='blue', ha='center', va='center')

    # Отрисовываем рёбра и подписываем их номера
    for edge in edges_data:
        node1_id, node2_id, edge_id = edge

        # Получаем координаты первой и второй вершин
        point1 = points[node1_id]
        point2 = points[node2_id]

        # Координаты для начала и конца ребра
        x_coords = [point1['X'], point2['X']]
        y_coords = [point1['Y'], point2['Y']]

        # Вычисляем середину ребра для размещения номера ребра
        mid_x = (point1['X'] + point2['X']) / 2
        mid_y = (point1['Y'] + point2['Y']) / 2

        # Добавляем текст с номером ребра в середину отрезка
        plt.text(mid_x, mid_y, f'{edge_id}', fontsize=10, color='red', ha='center', va='center')

    # Добавляем номера вершин
    for point_id, point in enumerate(points):
        # Добавляем текст с номером вершины рядом с вершиной (немного смещаем для лучшей видимости)
        plt.text(point['X'], point['Y'], f'{point_id}', fontsize=10, color='green', ha='right', va='top')

    # Настройка отображения
    plt.gca().set_aspect('equal', adjustable='box')
    plt.xlabel('X')
    plt.ylabel('Y')
    plt.title('Finite Element Grid with Edge, Element, and Vertex Numbers')
    plt.show()

def plot_grid(grid_data):
    points = grid_data['Nodes']
    elements = grid_data['Elements']

    # Создаём палитру цветов на основе AreaId
    colors = list(mcolors.XKCD_COLORS.values())  # Используем таблицу цветов из matplotlib

    # Для каждого элемента отрисуем его и закрасим в зависимости от AreaId, а также добавим номер элемента
    for element_id, element in enumerate(elements):
        node_ids = element['NodeIds']
        area_id = element['AreaId']

        # Получаем координаты узлов элемента в правильном порядке
        # Порядок: 0-я, 1-я, 3-я, 2-я
        element_points = [points[node_ids[i]] for i in [0, 1, 3, 2]]

        # Разделяем координаты X и Y
        x_coords = [p['X'] for p in element_points]
        y_coords = [p['Y'] for p in element_points]

        # Определяем цвет для элемента в зависимости от AreaId
        color = colors[area_id % len(colors)]  # Используем модуль, чтобы избежать выхода за пределы палитры

        # Закрашиваем элемент
        plt.fill(x_coords, y_coords,
                 color=color,
                 edgecolor='black', alpha=0.5)

        # Вычисляем центр элемента для размещения номера элемента
        center_x = sum(x_coords) / 4
        center_y = sum(y_coords) / 4

        # Добавляем текст с номером элемента в центр
        # plt.text(center_x, center_y, f'{element_id}', fontsize=10, color='blue', ha='center', va='center')

    # Настройка отображения
    plt.gca().set_aspect('equal', adjustable='box')
    plt.xlabel('X')
    plt.ylabel('Y')
    plt.title('Finite Element Grid')
    plt.show()

def plot_grid_with_solution(grid_data, solution, num_contour_lines=10):
    points = grid_data['Nodes']
    elements = grid_data['Elements']
    (x, y, T) = solution

    # Создаём 2D-сетку для интерполяции решения
    grid_x, grid_y = np.mgrid[min(x):max(x):100j, min(y):max(y):100j]
    grid_T = griddata((x, y), T, (grid_x, grid_y), method='linear')

    # Построение цветовой карты
    plt.contourf(grid_x, grid_y, grid_T, levels=100, cmap='coolwarm')
    plt.colorbar(label='U')

    # Построение изолиний с пунктирными линиями
    contours = plt.contour(
        grid_x,
        grid_y,
        grid_T,
        levels=num_contour_lines,
        colors='black',
        alpha=0.5,
        linewidths=1,
        linestyles='dashed'  # Настройка стиля линий: пунктир
    )
    # plt.clabel(contours, inline=True, fontsize=8, fmt="%.1f")  # Подписываем значения изолиний

    # Для каждого элемента отрисуем его границы
    for element_id, element in enumerate(elements):
        node_ids = element['NodeIds']

        # Получаем координаты узлов элемента в правильном порядке
        # Порядок: 0-я, 1-я, 3-я, 2-я
        element_points = [points[node_ids[i]] for i in [0, 1, 3, 2]]

        # Разделяем координаты X и Y
        x_coords = [p['X'] for p in element_points]
        y_coords = [p['Y'] for p in element_points]

        # Закрашиваем границу элемента с прозрачностью
        plt.fill(x_coords, y_coords, color=(1, 1, 1, 0), edgecolor='black')

    # Настройка отображения
    plt.gca().set_aspect('equal', adjustable='box')
    plt.xlabel('X')
    plt.ylabel('Y')
    plt.title('U Field with Isolines')
    plt.show()

# Загрузка данных из json-файла
grid_data = load_grid_from_json('curveGridSplitted.txt')

solution = load_solution('solution.txt', ',')

# Загрузка данных о рёбрах
edges_data = load_edges_from_file('edges.txt')

# Построение сетки с рёбрами, номерами рёбер, элементов и вершин
# plot_grid_with_solution(grid_data, solution)
plot_grid(grid_data)